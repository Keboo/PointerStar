import { createServer } from "node:http";
import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { CanvasError, createCanvas, joinSession } from "@github/copilot-sdk/extension";

const execFileAsync = promisify(execFile);

const DEFAULT_OWNER = "Keboo";
const DEFAULT_PROJECT_NUMBER = 6;
const DAY_MS = 24 * 60 * 60 * 1000;

const servers = new Map();
const canvasState = new Map();

const PROJECT_QUERY = `
query($owner: String!, $number: Int!, $cursor: String) {
  user(login: $owner) {
    projectV2(number: $number) {
      id
      title
      url
      fields(first: 50) {
        nodes {
          ... on ProjectV2Field {
            id
            name
            dataType
          }
          ... on ProjectV2SingleSelectField {
            id
            name
            dataType
            options {
              id
              name
              color
            }
          }
        }
      }
      items(first: 100, after: $cursor) {
        pageInfo {
          hasNextPage
          endCursor
        }
        nodes {
          id
          createdAt
          updatedAt
          fieldValues(first: 30) {
            nodes {
              ... on ProjectV2ItemFieldDateValue {
                date
                field {
                  ... on ProjectV2Field {
                    name
                  }
                }
              }
              ... on ProjectV2ItemFieldSingleSelectValue {
                name
                optionId
                field {
                  ... on ProjectV2SingleSelectField {
                    name
                  }
                }
              }
              ... on ProjectV2ItemFieldIterationValue {
                title
                startDate
                duration
                field {
                  ... on ProjectV2IterationField {
                    name
                  }
                }
              }
            }
          }
          content {
            __typename
            ... on DraftIssue {
              title
            }
            ... on Issue {
              title
              url
            }
            ... on PullRequest {
              title
              url
            }
          }
        }
      }
    }
  }
}
`;

function parseDate(rawDate) {
    if (!rawDate || typeof rawDate !== "string") {
        return null;
    }

    if (rawDate.length === 10) {
        return `${rawDate}T00:00:00.000Z`;
    }

    const parsed = new Date(rawDate);
    if (Number.isNaN(parsed.getTime())) {
        return null;
    }

    return parsed.toISOString();
}

function addDays(isoDate, days) {
    const parsed = new Date(isoDate);
    return new Date(parsed.getTime() + days * DAY_MS).toISOString();
}

function pickDateByName(dateValues, regex) {
    for (const value of dateValues) {
        const fieldName = (value.fieldName ?? "").toLowerCase();
        if (regex.test(fieldName) && value.date) {
            return value.date;
        }
    }
    return null;
}

function normalizeProjectInput(input) {
    const owner = typeof input?.owner === "string" && input.owner.trim().length > 0
        ? input.owner.trim()
        : DEFAULT_OWNER;
    const projectNumber = Number.isInteger(input?.projectNumber) && input.projectNumber > 0
        ? input.projectNumber
        : DEFAULT_PROJECT_NUMBER;
    return { owner, projectNumber };
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}

function createGhCliEnv() {
    const env = { ...process.env };
    delete env.GH_TOKEN;
    delete env.GITHUB_TOKEN;
    delete env.GH_ENTERPRISE_TOKEN;
    delete env.GITHUB_ENTERPRISE_TOKEN;
    return env;
}

function normalizeStatusColorKey(rawColor) {
    if (typeof rawColor !== "string" || rawColor.length === 0) {
        return null;
    }
    return rawColor.toLowerCase();
}

function normalizeDateOnly(dateValue) {
    if (typeof dateValue !== "string") {
        throw new CanvasError("invalid_date", "Date values must be strings in YYYY-MM-DD format.");
    }
    if (!/^\d{4}-\d{2}-\d{2}$/.test(dateValue)) {
        throw new CanvasError("invalid_date", `Invalid date format: ${dateValue}. Expected YYYY-MM-DD.`);
    }
    return dateValue;
}

async function runGhGraphql({ owner, projectNumber, cursor }) {
    const args = [
        "api",
        "graphql",
        "-f",
        `query=${PROJECT_QUERY}`,
        "-F",
        `owner=${owner}`,
        "-F",
        `number=${projectNumber}`,
    ];

    if (cursor) {
        args.push("-F", `cursor=${cursor}`);
    }

    try {
        const { stdout } = await execFileAsync("gh", args, {
            env: createGhCliEnv(),
            windowsHide: true,
            maxBuffer: 2 * 1024 * 1024,
        });
        return JSON.parse(stdout);
    } catch (error) {
        throw new CanvasError("project_fetch_failed", `Unable to query GitHub project data: ${error?.message ?? "unknown error"}`);
    }
}

async function updateProjectDateField({ projectId, itemId, fieldId, date }) {
    const mutation = `
mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $date: Date!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId,
    itemId: $itemId,
    fieldId: $fieldId,
    value: { date: $date }
  }) {
    projectV2Item { id }
  }
}
`;

    const args = [
        "api",
        "graphql",
        "-f",
        `query=${mutation}`,
        "-F",
        `projectId=${projectId}`,
        "-F",
        `itemId=${itemId}`,
        "-F",
        `fieldId=${fieldId}`,
        "-F",
        `date=${date}`,
    ];

    try {
        await execFileAsync("gh", args, {
            env: createGhCliEnv(),
            windowsHide: true,
            maxBuffer: 1024 * 1024,
        });
    } catch (error) {
        throw new CanvasError("project_update_failed", `Unable to update project item dates: ${error?.message ?? "unknown error"}`);
    }
}

function mapProjectItem(item, statusColorByOptionId, statusColorByName) {
    const title = item.content?.title ?? "Untitled item";
    const url = item.content?.url ?? null;
    const contentType = item.content?.__typename ?? "Unknown";

    const dateValues = [];
    const singleSelectValues = [];
    let iteration = null;

    for (const fieldValue of item.fieldValues?.nodes ?? []) {
        if (fieldValue?.date) {
            dateValues.push({
                fieldName: fieldValue.field?.name ?? "",
                date: parseDate(fieldValue.date),
            });
            continue;
        }

        if (fieldValue?.startDate) {
            iteration = {
                fieldName: fieldValue.field?.name ?? "Iteration",
                title: fieldValue.title ?? "Iteration",
                startDate: parseDate(fieldValue.startDate),
                duration: Number.isInteger(fieldValue.duration) ? fieldValue.duration : 7,
            };
            continue;
        }

        if (fieldValue?.name) {
            singleSelectValues.push({
                fieldName: fieldValue.field?.name ?? "",
                value: fieldValue.name,
                optionId: fieldValue.optionId ?? null,
            });
        }
    }

    const startFromDateField = pickDateByName(dateValues, /(start|begin|planned start|from)/i);
    const endFromDateField = pickDateByName(dateValues, /(due|target|end|finish|complete)/i);

    let start = startFromDateField ?? iteration?.startDate ?? parseDate(item.createdAt);
    let end = endFromDateField
        ?? (iteration?.startDate ? addDays(iteration.startDate, Math.max(iteration.duration - 1, 1)) : null)
        ?? parseDate(item.updatedAt);

    if (!start && end) {
        start = addDays(end, -1);
    }

    if (start && !end) {
        end = addDays(start, 1);
    }

    if (start && end && new Date(end).getTime() <= new Date(start).getTime()) {
        end = addDays(start, 1);
    }

    const statusValue = singleSelectValues.find((value) => value.fieldName.toLowerCase() === "status")?.value
        ?? singleSelectValues[0]?.value
        ?? null;
    const statusOptionId = singleSelectValues.find((value) => value.fieldName.toLowerCase() === "status")?.optionId
        ?? singleSelectValues[0]?.optionId
        ?? null;
    const statusColor = (statusOptionId && statusColorByOptionId.get(statusOptionId))
        ?? (statusValue ? statusColorByName.get(statusValue) : null)
        ?? null;

    return {
        id: item.id,
        title,
        url,
        type: contentType,
        status: statusValue,
        statusColor,
        start,
        end,
        dateFields: dateValues.filter((value) => value.date),
        iteration,
    };
}

async function loadProjectData(projectInput) {
    let cursor = null;
    let hasNextPage = true;
    const allItems = [];
    let projectTitle = "GitHub Project";
    let projectUrl = null;
    let projectId = null;
    let startDateFieldId = null;
    let targetDateFieldId = null;
    const statusColorByOptionId = new Map();
    const statusColorByName = new Map();

    while (hasNextPage) {
        const response = await runGhGraphql({ ...projectInput, cursor });
        const project = response?.data?.user?.projectV2;
        if (!project) {
            throw new CanvasError(
                "project_not_found",
                `Could not find project ${projectInput.owner}/${projectInput.projectNumber}. Make sure the board exists and your gh auth has access.`,
            );
        }

        projectTitle = project.title ?? projectTitle;
        projectUrl = project.url ?? projectUrl;
        projectId = project.id ?? projectId;
        const fieldNodes = project.fields?.nodes ?? [];
        const startField = fieldNodes.find((field) => field?.name?.toLowerCase() === "start date");
        const targetField = fieldNodes.find((field) => field?.name?.toLowerCase() === "target date");
        startDateFieldId = startField?.id ?? startDateFieldId;
        targetDateFieldId = targetField?.id ?? targetDateFieldId;
        const statusField = (project.fields?.nodes ?? []).find((field) => field?.name?.toLowerCase() === "status");
        if (statusField?.options) {
            for (const option of statusField.options) {
                const colorKey = normalizeStatusColorKey(option?.color);
                if (!colorKey) {
                    continue;
                }
                if (option?.id) {
                    statusColorByOptionId.set(option.id, colorKey);
                }
                if (option?.name) {
                    statusColorByName.set(option.name, colorKey);
                }
            }
        }

        const nodes = project.items?.nodes ?? [];
        allItems.push(...nodes.map((item) => mapProjectItem(item, statusColorByOptionId, statusColorByName)));

        const pageInfo = project.items?.pageInfo;
        hasNextPage = Boolean(pageInfo?.hasNextPage);
        cursor = pageInfo?.endCursor ?? null;
    }

    const itemsWithDates = allItems
        .filter((item) => item.start && item.end)
        .sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime());

    const minStart = itemsWithDates.length > 0 ? itemsWithDates[0].start : null;
    const maxEnd = itemsWithDates.length > 0 ? itemsWithDates[itemsWithDates.length - 1].end : null;

    return {
        fetchedAt: new Date().toISOString(),
        projectId,
        startDateFieldId,
        targetDateFieldId,
        owner: projectInput.owner,
        projectNumber: projectInput.projectNumber,
        projectTitle,
        projectUrl,
        totalItems: allItems.length,
        itemsWithDates: itemsWithDates.length,
        minStart,
        maxEnd,
        items: itemsWithDates,
    };
}

function renderHtml(instanceId) {
    return `<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Project Gantt</title>
    <style>
      body {
        margin: 0;
        background: var(--background-color-default, #ffffff);
        color: var(--text-color-default, #1f2328);
        font-family: var(--font-sans, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif);
      }
      .layout {
        padding: 16px;
      }
      .header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 8px;
        margin-bottom: 12px;
      }
      .title {
        font-size: 18px;
        font-weight: 600;
      }
      .subtitle {
        color: var(--text-color-muted, #656d76);
        font-size: 12px;
      }
      .button {
        appearance: none;
        border: 1px solid var(--border-color-default, #d1d9e0);
        border-radius: 6px;
        background: transparent;
        color: inherit;
        padding: 6px 10px;
        cursor: pointer;
      }
      .button:hover {
        background: rgba(128, 128, 128, 0.1);
      }
      .filters {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        margin-bottom: 12px;
      }
      .filter-item {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 4px 8px;
        border: 1px solid var(--border-color-default, #d1d9e0);
        border-radius: 999px;
        font-size: 12px;
        user-select: none;
      }
      .filter-item input {
        margin: 0;
      }
      .grid {
        display: grid;
        grid-template-columns: minmax(220px, 320px) 1fr;
        border: 1px solid var(--border-color-default, #d1d9e0);
        border-radius: 8px;
        overflow: hidden;
      }
      .col-header {
        padding: 8px 10px;
        font-size: 12px;
        font-weight: 600;
        border-bottom: 1px solid var(--border-color-default, #d1d9e0);
        background: rgba(128, 128, 128, 0.08);
      }
      .timeline-header {
        position: relative;
        min-height: 36px;
        padding: 4px 8px 6px;
      }
      .axis-line {
        position: absolute;
        left: 8px;
        right: 8px;
        bottom: 6px;
        border-top: 1px solid var(--border-color-default, #d1d9e0);
      }
      .axis-tick {
        position: absolute;
        bottom: 6px;
        transform: translateX(-50%);
        font-size: 10px;
        color: var(--text-color-muted, #656d76);
        white-space: nowrap;
      }
      .axis-tick::before {
        content: "";
        position: absolute;
        left: 50%;
        transform: translateX(-50%);
        bottom: -6px;
        width: 1px;
        height: 6px;
        background: var(--border-color-default, #d1d9e0);
      }
      .today-line {
        position: absolute;
        top: 0;
        bottom: 0;
        width: 2px;
        background: var(--true-color-red, #cf222e);
        opacity: 0.75;
        pointer-events: none;
      }
      .today-label {
        position: absolute;
        top: 0;
        transform: translate(-50%, -100%);
        font-size: 10px;
        color: var(--true-color-red, #cf222e);
        white-space: nowrap;
        pointer-events: none;
      }
      .rows {
        grid-column: 1 / -1;
      }
      .row {
        display: grid;
        grid-template-columns: minmax(220px, 320px) 1fr;
        min-height: 34px;
        border-bottom: 1px solid var(--border-color-default, #d1d9e0);
      }
      .row:last-child {
        border-bottom: 0;
      }
      .item {
        padding: 8px 10px;
        display: flex;
        flex-direction: column;
        justify-content: center;
        gap: 2px;
      }
      .item a {
        color: inherit;
        text-decoration: none;
      }
      .item a:hover {
        text-decoration: underline;
      }
      .meta {
        color: var(--text-color-muted, #656d76);
        font-size: 11px;
      }
      .timeline {
        position: relative;
        padding: 7px 8px;
      }
      .bar-wrap {
        position: absolute;
        top: 7px;
        height: 20px;
        cursor: grab;
      }
      .bar-wrap.is-dragging {
        cursor: grabbing;
      }
      .bar-fill {
        position: absolute;
        left: 0;
        right: 0;
        top: 1px;
        bottom: 1px;
        border-radius: 6px;
        border: 1px solid var(--true-color-blue, #0969da);
      }
      .bar-handle {
        position: absolute;
        top: 0;
        width: 6px;
        height: 20px;
        background: color-mix(in srgb, var(--text-color-default, #1f2328) 40%, transparent);
        border-radius: 3px;
        opacity: 0;
        transition: opacity 120ms ease-in-out;
        cursor: ew-resize;
      }
      .bar-wrap:hover .bar-handle,
      .bar-wrap.is-dragging .bar-handle {
        opacity: 1;
      }
      .bar-handle.left {
        left: -2px;
      }
      .bar-handle.right {
        right: -2px;
      }
      .empty {
        padding: 16px;
        border: 1px dashed var(--border-color-default, #d1d9e0);
        border-radius: 8px;
        color: var(--text-color-muted, #656d76);
      }
      .error {
        color: var(--true-color-red, #cf222e);
      }
    </style>
  </head>
  <body>
    <div class="layout">
      <div class="header">
        <div>
          <div class="title">Project Gantt</div>
          <div id="summary" class="subtitle">Loading...</div>
        </div>
        <button id="refreshBtn" class="button" type="button">Refresh</button>
      </div>
      <div id="content"></div>
    </div>
    <script>
      const instanceId = ${JSON.stringify(instanceId)};
      const DEFAULT_HIDDEN_STATUSES = new Set(["Backlog", "Done"]);
      const DAY_MS = 24 * 60 * 60 * 1000;
      let currentData = null;
      let selectedStatuses = null;
      let knownStatuses = new Set();
      let currentTimelineBounds = null;
      let dragState = null;
      const STATUS_ORDER = ["Backlog", "Ready", "In progress", "In review", "Done", "Unknown"];

      function sortStatuses(statuses) {
        return [...statuses].sort((a, b) => {
          const indexA = STATUS_ORDER.indexOf(a);
          const indexB = STATUS_ORDER.indexOf(b);
          const rankA = indexA === -1 ? 999 : indexA;
          const rankB = indexB === -1 ? 999 : indexB;
          if (rankA !== rankB) {
            return rankA - rankB;
          }
          return a.localeCompare(b);
        });
      }

      function ensureSelectedStatuses(allStatuses) {
        if (selectedStatuses === null) {
          selectedStatuses = new Set(allStatuses.filter((status) => !DEFAULT_HIDDEN_STATUSES.has(status)));
          knownStatuses = new Set(allStatuses);
          return;
        }

        for (const status of allStatuses) {
          if (!selectedStatuses.has(status) && !DEFAULT_HIDDEN_STATUSES.has(status) && !knownStatuses.has(status)) {
            selectedStatuses.add(status);
          }
        }
        knownStatuses = new Set([...knownStatuses, ...allStatuses]);
      }

      function render(data) {
        const summaryEl = document.getElementById("summary");
        const contentEl = document.getElementById("content");
        currentData = data;

        if (data.error) {
          summaryEl.textContent = "Unable to load data";
          contentEl.innerHTML = '<div class="empty error">' + data.error + '</div>';
          return;
        }

        const fetchedAt = new Date(data.fetchedAt).toLocaleString();
        const projectTitle = data.projectTitle || "Project";
        const projectLink = data.projectUrl ? ' <a href="' + data.projectUrl + '" target="_blank" rel="noreferrer">Open project</a>' : "";
        summaryEl.innerHTML = projectTitle + " · " + data.itemsWithDates + "/" + data.totalItems + " dated items · Updated " + fetchedAt + projectLink;

        if (!Array.isArray(data.items) || data.items.length === 0) {
          contentEl.innerHTML = '<div class="empty">No project items with date information were found.</div>';
          return;
        }

        const allStatuses = sortStatuses(new Set(data.items.map((item) => item.status || "Unknown")));
        ensureSelectedStatuses(allStatuses);
        const activeStatuses = new Set(allStatuses.filter((status) => selectedStatuses.has(status)));
        const filteredItems = data.items.filter((item) => activeStatuses.has(item.status || "Unknown"));

        if (filteredItems.length === 0) {
          const filtersHtml = renderFilters(allStatuses, activeStatuses);
          contentEl.innerHTML = filtersHtml + '<div class="empty">No items match the selected columns.</div>';
          wireFilterControls(allStatuses);
          return;
        }

        const min = Math.min(...filteredItems.map((item) => new Date(item.start).getTime()));
        const max = Math.max(...filteredItems.map((item) => new Date(item.end).getTime()));
        const totalSpan = Math.max(max - min, 1);
        currentTimelineBounds = { min, max, totalSpan };
        const tickCount = 6;
        const ticks = Array.from({ length: tickCount }, (_, i) => {
          const ratio = i / (tickCount - 1);
          const dateMs = min + Math.round(totalSpan * ratio);
          return {
            leftPct: ratio * 100,
            label: formatAxisDate(dateMs)
          };
        });
        const axisTicksHtml = ticks.map((tick) =>
          '<div class="axis-tick" style="left:' + tick.leftPct.toFixed(3) + '%;">' + escapeHtml(tick.label) + '</div>'
        ).join("");
        const todayMs = Date.now();
        const todayInRange = todayMs >= min && todayMs <= max;
        const todayLeftPct = ((todayMs - min) / totalSpan) * 100;
        const todayHeaderHtml = todayInRange
          ? '<div class="today-line" style="left:' + todayLeftPct.toFixed(3) + '%;"></div><div class="today-label" style="left:' + todayLeftPct.toFixed(3) + '%;">Today</div>'
          : "";

        const rows = filteredItems.map((item) => {
          const start = new Date(item.start).getTime();
          const end = new Date(item.end).getTime();
          const leftPct = ((start - min) / totalSpan) * 100;
          const widthPct = Math.max(((end - start) / totalSpan) * 100, 1);
          const rangeLabel = item.start.slice(0,10) + ' → ' + item.end.slice(0,10);
          const label = item.status ? item.type + " · " + item.status : item.type;
          const colorTokens = getStatusColorTokens(item.statusColor);
          const barFillStyle = 'background:' + colorTokens.background + '; border-color:' + colorTokens.border + ';';
          const metaStyle = item.statusColor ? ('style="color:' + escapeHtml(colorTokens.border) + ';"') : "";
          const titleCell = item.url
            ? '<a href="' + item.url + '" target="_blank" rel="noreferrer">' + escapeHtml(item.title) + '</a>'
            : escapeHtml(item.title);
          return '<div class="row">' +
            '<div class="item">' +
              '<div>' + titleCell + '</div>' +
              '<div class="meta" ' + metaStyle + '>' + escapeHtml(label + " · " + rangeLabel) + '</div>' +
            '</div>' +
            '<div class="timeline">' +
              (todayInRange ? '<div class="today-line" style="left:' + todayLeftPct.toFixed(3) + '%;"></div>' : '') +
              '<div class="bar-wrap" data-item-id="' + escapeHtml(item.id) + '" data-start="' + escapeHtml(item.start) + '" data-end="' + escapeHtml(item.end) + '" style="left:' + leftPct.toFixed(3) + '%; width:' + widthPct.toFixed(3) + '%;" title="' + escapeHtml(item.start.slice(0,10) + " → " + item.end.slice(0,10)) + '">' +
                '<div class="bar-fill" style="' + barFillStyle + '"></div>' +
                '<div class="bar-handle left" data-resize="left"></div>' +
                '<div class="bar-handle right" data-resize="right"></div>' +
              '</div>' +
            '</div>' +
          '</div>';
        }).join("");

        const filtersHtml = renderFilters(allStatuses, activeStatuses);
        contentEl.innerHTML =
          filtersHtml +
          '<div class="grid">' +
            '<div class="col-header">Item</div>' +
            '<div class="col-header timeline-header">Timeline<div class="axis-line"></div>' + axisTicksHtml + todayHeaderHtml + '</div>' +
            '<div class="rows">' + rows + '</div>' +
          '</div>';
        wireFilterControls(allStatuses);
        wireBarInteractions();
      }

      function renderFilters(allStatuses, activeStatuses) {
        const controls = allStatuses.map((status) => {
          const checked = activeStatuses.has(status) ? " checked" : "";
          return '<label class="filter-item">' +
            '<input type="checkbox" data-status="' + escapeHtml(status) + '"' + checked + ' />' +
            '<span>' + escapeHtml(status) + '</span>' +
          '</label>';
        }).join("");
        return '<div class="filters">' + controls + '</div>';
      }

      function wireFilterControls(allStatuses) {
        const checkboxes = document.querySelectorAll('.filters input[type="checkbox"][data-status]');
        checkboxes.forEach((checkbox) => {
          checkbox.addEventListener("change", () => {
            const nextSelected = new Set();
            checkboxes.forEach((cb) => {
              if (cb.checked) {
                nextSelected.add(cb.getAttribute("data-status") || "");
              }
            });
            selectedStatuses = new Set([...nextSelected].filter((status) => status && allStatuses.includes(status)));
            if (currentData) {
              render(currentData);
            }
          });
        });
      }

      function getStatusColorTokens(statusColor) {
        switch ((statusColor || "").toLowerCase()) {
          case "red":
            return { background: "var(--true-color-red-muted, #ff818266)", border: "var(--true-color-red, #cf222e)" };
          case "orange":
            return { background: "var(--true-color-orange-muted, #fb8f4466)", border: "var(--true-color-orange, #bc4c00)" };
          case "yellow":
            return { background: "var(--true-color-yellow-muted, #d4a72c66)", border: "var(--true-color-yellow, #9a6700)" };
          case "green":
            return { background: "var(--true-color-green-muted, #3fb95066)", border: "var(--true-color-green, #1a7f37)" };
          case "blue":
            return { background: "var(--true-color-blue-muted, #54aeff80)", border: "var(--true-color-blue, #0969da)" };
          case "purple":
            return { background: "var(--true-color-purple-muted, #a371f766)", border: "var(--true-color-purple, #8250df)" };
          case "pink":
            return { background: "var(--true-color-pink-muted, #f778ba66)", border: "var(--true-color-pink, #bf3989)" };
          case "gray":
          case "grey":
            return { background: "rgba(101, 109, 118, 0.35)", border: "var(--text-color-muted, #656d76)" };
          default:
            return { background: "var(--true-color-blue-muted, #54aeff80)", border: "var(--true-color-blue, #0969da)" };
        }
      }

      function wireBarInteractions() {
        const bars = document.querySelectorAll(".bar-wrap[data-item-id]");
        bars.forEach((bar) => {
          bar.addEventListener("pointerdown", onBarPointerDown);
        });
      }

      function onBarPointerDown(event) {
        const bar = event.currentTarget;
        if (!bar || !currentTimelineBounds) {
          return;
        }

        const itemId = bar.getAttribute("data-item-id");
        const startIso = bar.getAttribute("data-start");
        const endIso = bar.getAttribute("data-end");
        if (!itemId || !startIso || !endIso) {
          return;
        }

        const startMs = new Date(startIso).getTime();
        const endMs = new Date(endIso).getTime();
        if (Number.isNaN(startMs) || Number.isNaN(endMs)) {
          return;
        }

        const resizeMode = event.target?.getAttribute?.("data-resize");
        const mode = resizeMode === "left" || resizeMode === "right" ? resizeMode : "move";
        const timeline = bar.closest(".timeline");
        if (!timeline) {
          return;
        }

        dragState = {
          bar,
          timeline,
          mode,
          itemId,
          originX: event.clientX,
          originalStartMs: startMs,
          originalEndMs: endMs,
        };

        bar.classList.add("is-dragging");
        document.body.style.userSelect = "none";
        bar.setPointerCapture(event.pointerId);
        bar.addEventListener("pointermove", onBarPointerMove);
        bar.addEventListener("pointerup", onBarPointerUp);
        bar.addEventListener("pointercancel", onBarPointerCancel);
      }

      function onBarPointerMove(event) {
        if (!dragState || !currentTimelineBounds) {
          return;
        }

        const timelineWidth = dragState.timeline.getBoundingClientRect().width;
        if (timelineWidth <= 0) {
          return;
        }

        const deltaPx = event.clientX - dragState.originX;
        const deltaMs = (deltaPx / timelineWidth) * currentTimelineBounds.totalSpan;
        const minDurationMs = DAY_MS;
        let nextStartMs = dragState.originalStartMs;
        let nextEndMs = dragState.originalEndMs;

        if (dragState.mode === "move") {
          nextStartMs += deltaMs;
          nextEndMs += deltaMs;
        } else if (dragState.mode === "left") {
          nextStartMs += deltaMs;
          if (nextStartMs > nextEndMs - minDurationMs) {
            nextStartMs = nextEndMs - minDurationMs;
          }
        } else if (dragState.mode === "right") {
          nextEndMs += deltaMs;
          if (nextEndMs < nextStartMs + minDurationMs) {
            nextEndMs = nextStartMs + minDurationMs;
          }
        }

        applyBarPreview(dragState.bar, nextStartMs, nextEndMs);
      }

      function onBarPointerUp(event) {
        if (!dragState || !currentTimelineBounds) {
          return;
        }

        const bar = dragState.bar;
        const itemId = dragState.itemId;
        const startIso = bar.getAttribute("data-start");
        const endIso = bar.getAttribute("data-end");
        const previewStart = bar.getAttribute("data-preview-start");
        const previewEnd = bar.getAttribute("data-preview-end");
        const hasPreview = previewStart && previewEnd;

        cleanupDrag(event.pointerId);

        if (!hasPreview || !startIso || !endIso) {
          return;
        }

        const newStartIsoDate = toIsoDateOnly(new Date(Number(previewStart)));
        const newEndIsoDate = toIsoDateOnly(new Date(Number(previewEnd)));
        const oldStartIsoDate = startIso.slice(0, 10);
        const oldEndIsoDate = endIso.slice(0, 10);

        if (newStartIsoDate === oldStartIsoDate && newEndIsoDate === oldEndIsoDate) {
          if (currentData) {
            render(currentData);
          }
          return;
        }

        saveItemDates(itemId, newStartIsoDate, newEndIsoDate);
      }

      function onBarPointerCancel(event) {
        cleanupDrag(event.pointerId);
        if (currentData) {
          render(currentData);
        }
      }

      function cleanupDrag(pointerId) {
        if (!dragState) {
          return;
        }
        const bar = dragState.bar;
        bar.classList.remove("is-dragging");
        try {
          bar.releasePointerCapture(pointerId);
        } catch {}
        bar.removeEventListener("pointermove", onBarPointerMove);
        bar.removeEventListener("pointerup", onBarPointerUp);
        bar.removeEventListener("pointercancel", onBarPointerCancel);
        document.body.style.userSelect = "";
        dragState = null;
      }

      function applyBarPreview(bar, startMs, endMs) {
        const startPct = ((startMs - currentTimelineBounds.min) / currentTimelineBounds.totalSpan) * 100;
        const endPct = ((endMs - currentTimelineBounds.min) / currentTimelineBounds.totalSpan) * 100;
        const widthPct = Math.max(endPct - startPct, 0.5);
        bar.style.left = startPct.toFixed(3) + "%";
        bar.style.width = widthPct.toFixed(3) + "%";
        bar.setAttribute("data-preview-start", String(startMs));
        bar.setAttribute("data-preview-end", String(endMs));
      }

      async function saveItemDates(itemId, startDate, targetDate) {
        try {
          const response = await fetch("/api/item-date", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ itemId, startDate, targetDate }),
          });

          if (!response.ok) {
            const errorPayload = await response.json().catch(() => ({}));
            throw new Error(errorPayload.error || "Failed to save date change.");
          }

          if (!currentData || !Array.isArray(currentData.items)) {
            await load();
            return;
          }

          const item = currentData.items.find((entry) => entry.id === itemId);
          if (item) {
            item.start = startDate + "T00:00:00.000Z";
            item.end = targetDate + "T00:00:00.000Z";
            currentData.items = [...currentData.items].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime());
            currentData.minStart = currentData.items[0]?.start ?? currentData.minStart;
            currentData.maxEnd = currentData.items[currentData.items.length - 1]?.end ?? currentData.maxEnd;
            currentData.fetchedAt = new Date().toISOString();
            render(currentData);
            return;
          }

          await load();
        } catch (error) {
          const summaryEl = document.getElementById("summary");
          if (summaryEl) {
            summaryEl.textContent = "Failed to save date update. Click Refresh and try again.";
          }
          await load();
        }
      }

      function toIsoDateOnly(date) {
        return new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate())).toISOString().slice(0, 10);
      }

      function formatAxisDate(dateMs) {
        const date = new Date(dateMs);
        return date.toLocaleDateString(undefined, { month: "short", day: "numeric" });
      }

      function escapeHtml(value) {
        return String(value)
          .replaceAll("&", "&amp;")
          .replaceAll("<", "&lt;")
          .replaceAll(">", "&gt;")
          .replaceAll('"', "&quot;")
          .replaceAll("'", "&#39;");
      }

      async function load() {
        try {
          const response = await fetch("/api/data");
          const data = await response.json();
          render(data);
        } catch (error) {
          render({ error: "Failed to load data." });
        }
      }

      document.getElementById("refreshBtn").addEventListener("click", async () => {
        await fetch("/api/refresh", { method: "POST" });
        await load();
      });

      load();
    </script>
  </body>
</html>`;
}

async function refreshState(instanceId) {
    const state = canvasState.get(instanceId);
    if (!state) {
        throw new CanvasError("instance_not_found", "Canvas instance was not initialized.");
    }
    const data = await loadProjectData(state.projectInput);
    state.data = data;
    state.error = null;
    return data;
}

async function readJsonBody(req) {
    const chunks = [];
    for await (const chunk of req) {
        chunks.push(chunk);
    }
    const raw = Buffer.concat(chunks).toString("utf8");
    if (!raw) {
        return {};
    }
    try {
        return JSON.parse(raw);
    } catch {
        throw new CanvasError("invalid_json", "Request body must be valid JSON.");
    }
}

async function startServer(instanceId) {
    const server = createServer(async (req, res) => {
        const url = new URL(req.url ?? "/", "http://127.0.0.1");
        if (url.pathname === "/") {
            res.setHeader("Content-Type", "text/html; charset=utf-8");
            res.end(renderHtml(instanceId));
            return;
        }

        if (url.pathname === "/api/data") {
            const state = canvasState.get(instanceId);
            if (!state) {
                res.statusCode = 404;
                res.setHeader("Content-Type", "application/json; charset=utf-8");
                res.end(JSON.stringify({ error: "Canvas state not found." }));
                return;
            }

            if (!state.data && !state.error) {
                try {
                    await refreshState(instanceId);
                } catch (error) {
                    state.error = error?.message ?? "Unknown error";
                }
            }

            res.setHeader("Content-Type", "application/json; charset=utf-8");
            if (state.error) {
                res.end(JSON.stringify({ error: state.error }));
                return;
            }

            res.end(JSON.stringify(state.data));
            return;
        }

        if (url.pathname === "/api/refresh" && req.method === "POST") {
            const state = canvasState.get(instanceId);
            if (!state) {
                res.statusCode = 404;
                res.setHeader("Content-Type", "application/json; charset=utf-8");
                res.end(JSON.stringify({ error: "Canvas state not found." }));
                return;
            }

            try {
                const data = await refreshState(instanceId);
                res.setHeader("Content-Type", "application/json; charset=utf-8");
                res.end(JSON.stringify({ refreshedAt: data.fetchedAt }));
            } catch (error) {
                state.error = error?.message ?? "Unknown error";
                res.statusCode = 500;
                res.setHeader("Content-Type", "application/json; charset=utf-8");
                res.end(JSON.stringify({ error: state.error }));
            }
            return;
        }

        if (url.pathname === "/api/item-date" && req.method === "POST") {
            const state = canvasState.get(instanceId);
            if (!state) {
                res.statusCode = 404;
                res.setHeader("Content-Type", "application/json; charset=utf-8");
                res.end(JSON.stringify({ error: "Canvas state not found." }));
                return;
            }

            try {
                const payload = await readJsonBody(req);
                const itemId = typeof payload.itemId === "string" ? payload.itemId : null;
                const startDate = normalizeDateOnly(payload.startDate);
                const targetDate = normalizeDateOnly(payload.targetDate);
                if (!itemId) {
                    throw new CanvasError("invalid_item", "itemId is required.");
                }
                if (!state.data?.projectId || !state.data?.startDateFieldId || !state.data?.targetDateFieldId) {
                    throw new CanvasError("project_fields_missing", "Missing project date field metadata for updates.");
                }
                if (new Date(targetDate).getTime() <= new Date(startDate).getTime()) {
                    throw new CanvasError("invalid_range", "Target date must be later than start date.");
                }

                await updateProjectDateField({
                    projectId: state.data.projectId,
                    itemId,
                    fieldId: state.data.startDateFieldId,
                    date: startDate,
                });
                await updateProjectDateField({
                    projectId: state.data.projectId,
                    itemId,
                    fieldId: state.data.targetDateFieldId,
                    date: targetDate,
                });

                if (Array.isArray(state.data.items)) {
                    const match = state.data.items.find((item) => item.id === itemId);
                    if (match) {
                        match.start = `${startDate}T00:00:00.000Z`;
                        match.end = `${targetDate}T00:00:00.000Z`;
                        state.data.items = [...state.data.items].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime());
                        state.data.minStart = state.data.items[0]?.start ?? state.data.minStart;
                        state.data.maxEnd = state.data.items[state.data.items.length - 1]?.end ?? state.data.maxEnd;
                        state.data.fetchedAt = new Date().toISOString();
                    }
                }

                res.setHeader("Content-Type", "application/json; charset=utf-8");
                res.end(JSON.stringify({ itemId, startDate, targetDate }));
            } catch (error) {
                res.statusCode = 500;
                res.setHeader("Content-Type", "application/json; charset=utf-8");
                res.end(JSON.stringify({ error: error?.message ?? "Unknown error" }));
            }
            return;
        }

        res.statusCode = 404;
        res.end("Not found");
    });

    await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
    const address = server.address();
    const port = typeof address === "object" && address ? address.port : 0;
    return { server, url: `http://127.0.0.1:${port}/` };
}

const session = await joinSession({
    canvases: [
        createCanvas({
            id: "project-gantt",
            displayName: "Project Gantt",
            description: "Renders a Gantt timeline from GitHub Project items (defaults to Keboo project #6).",
            inputSchema: {
                type: "object",
                additionalProperties: false,
                properties: {
                    owner: {
                        type: "string",
                        description: "GitHub user login that owns the project.",
                    },
                    projectNumber: {
                        type: "integer",
                        minimum: 1,
                        description: "GitHub project number under the owner.",
                    },
                },
            },
            actions: [
                {
                    name: "refresh",
                    description: "Refresh project items from GitHub and return summary counts.",
                    handler: async (ctx) => {
                        const data = await refreshState(ctx.instanceId);
                        return {
                            owner: data.owner,
                            projectNumber: data.projectNumber,
                            totalItems: data.totalItems,
                            itemsWithDates: data.itemsWithDates,
                            fetchedAt: data.fetchedAt,
                        };
                    },
                },
            ],
            open: async (ctx) => {
                const projectInput = normalizeProjectInput(ctx.input);
                let entry = servers.get(ctx.instanceId);
                if (!entry) {
                    entry = await startServer(ctx.instanceId);
                    servers.set(ctx.instanceId, entry);
                }

                canvasState.set(ctx.instanceId, {
                    projectInput,
                    data: null,
                    error: null,
                });

                try {
                    await refreshState(ctx.instanceId);
                } catch (error) {
                    const state = canvasState.get(ctx.instanceId);
                    if (state) {
                        state.error = error?.message ?? "Unknown error";
                    }
                }

                return {
                    title: `${projectInput.owner} Project #${projectInput.projectNumber} Gantt`,
                    status: `${projectInput.owner}#${projectInput.projectNumber}`,
                    url: entry.url,
                };
            },
            onClose: async (ctx) => {
                const entry = servers.get(ctx.instanceId);
                if (entry) {
                    servers.delete(ctx.instanceId);
                    await new Promise((resolve) => entry.server.close(() => resolve()));
                }
                canvasState.delete(ctx.instanceId);
            },
        }),
    ],
});
