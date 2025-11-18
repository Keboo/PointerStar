# PointerStar Application Insights - Kusto Queries

This document contains Kusto (KQL) queries for analyzing metrics in Application Insights.

## Prerequisites

These queries assume you have configured Application Insights with the connection string in `appsettings.json` and the application is tracking the following custom events:
- `RoomCreated` - When a new room is created
- `UserConnected` - When a user connects to a room
- `UserDisconnected` - When a user disconnects from a room
- `VotesReset` - When votes are reset (indicates active pointing session)

## Query 1: Number of Pointing Rooms Created Per Month

This query counts the number of unique rooms created, broken down by month.

```kusto
customEvents
| where name == "RoomCreated"
| extend RoomId = tostring(customDimensions.RoomId)
| summarize UniqueRooms = dcount(RoomId) by Month = startofmonth(timestamp)
| order by Month desc
```

## Query 2: Maximum Number of Users Connected to a Given Room

This query calculates the maximum concurrent users in each room by tracking connection and disconnection events.

```kusto
let connections = customEvents
    | where name == "UserConnected"
    | extend RoomId = tostring(customDimensions.RoomId), UserId = tostring(customDimensions.UserId)
    | project timestamp, RoomId, UserId, Delta = 1;
let disconnections = customEvents
    | where name == "UserDisconnected"
    | extend RoomId = tostring(customDimensions.RoomId), UserId = tostring(customDimensions.UserId)
    | project timestamp, RoomId, UserId, Delta = -1;
union connections, disconnections
| order by RoomId, timestamp asc
| extend RunningTotal = row_cumsum(Delta, RoomId != prev(RoomId))
| summarize MaxUsers = max(RunningTotal) by RoomId, Month = startofmonth(timestamp)
| order by Month desc, MaxUsers desc
```

**Alternative simpler approach using metrics:**

```kusto
customMetrics
| where name == "RoomUserCount"
| extend RoomId = tostring(customDimensions.RoomId)
| summarize MaxUsers = max(value) by RoomId, Month = startofmonth(timestamp)
| order by Month desc, MaxUsers desc
```

## Query 3: Average Time Room is Active Per Month

This query calculates room activity duration based on the time between first user connection and last user disconnection.

```kusto
let roomActivity = customEvents
    | where name in ("UserConnected", "UserDisconnected")
    | extend RoomId = tostring(customDimensions.RoomId)
    | summarize 
        FirstActivity = min(timestamp), 
        LastActivity = max(timestamp),
        EventCount = count()
        by RoomId
    | where EventCount >= 2  // Filter out rooms with only 1 event
    | extend DurationMinutes = datetime_diff('minute', LastActivity, FirstActivity);
roomActivity
| summarize 
    AvgDurationMinutes = avg(DurationMinutes),
    TotalRooms = count()
    by Month = startofmonth(FirstActivity)
| order by Month desc
```

## Query 4: Number of Pointing Sessions Per Month

This query counts the number of times votes were reset (each reset indicates a new pointing session).

```kusto
customEvents
| where name == "VotesReset"
| extend RoomId = tostring(customDimensions.RoomId)
| summarize PointingSessions = count() by Month = startofmonth(timestamp)
| order by Month desc
```

## Query 5: Average Pointing Sessions Per Room Per Month

```kusto
customEvents
| where name == "VotesReset"
| extend RoomId = tostring(customDimensions.RoomId)
| summarize 
    TotalSessions = count(),
    UniqueRooms = dcount(RoomId)
    by Month = startofmonth(timestamp)
| extend AvgSessionsPerRoom = TotalSessions * 1.0 / UniqueRooms
| order by Month desc
```

## Query 6: Room Creation and Activity Dashboard

Combined view of room creation, usage, and activity metrics per month.

```kusto
let roomCreations = customEvents
    | where name == "RoomCreated"
    | extend RoomId = tostring(customDimensions.RoomId)
    | summarize RoomsCreated = dcount(RoomId) by Month = startofmonth(timestamp);
let maxUsers = customMetrics
    | where name == "RoomUserCount"
    | extend RoomId = tostring(customDimensions.RoomId)
    | summarize AvgMaxUsers = avg(value), PeakUsers = max(value) by Month = startofmonth(timestamp);
let sessions = customEvents
    | where name == "VotesReset"
    | summarize PointingSessions = count() by Month = startofmonth(timestamp);
let roomActivity = customEvents
    | where name in ("UserConnected", "UserDisconnected")
    | extend RoomId = tostring(customDimensions.RoomId)
    | summarize 
        FirstActivity = min(timestamp), 
        LastActivity = max(timestamp),
        EventCount = count()
        by RoomId
    | where EventCount >= 2
    | extend DurationMinutes = datetime_diff('minute', LastActivity, FirstActivity)
    | summarize AvgDurationMinutes = avg(DurationMinutes) by Month = startofmonth(FirstActivity);
roomCreations
| join kind=fullouter maxUsers on Month
| join kind=fullouter sessions on Month
| join kind=fullouter roomActivity on Month
| project 
    ["Month"] = format_datetime(Month, 'M/yyyy'),
    ["Rooms Created"] = coalesce(RoomsCreated, long(0)),
    ["Avg Users in Room"] = round(coalesce(AvgMaxUsers, real(0.0)), 1),
    ["Peak Users in Room"] = coalesce(PeakUsers, real(0)),
    ["Pointing Sessions"] = coalesce(PointingSessions, long(0)),
    ["Avg Room Duration"] = round(coalesce(AvgDurationMinutes, real(0.0)), 1)
| order by Month desc
```

## Usage Notes

1. All queries use the `timestamp` field which is automatically added by Application Insights
2. Queries use `customDimensions` to access properties attached to events (RoomId, UserId, etc.)
3. The `customMetrics` table is used for the `RoomUserCount` metric which tracks concurrent users
4. Time ranges can be adjusted using the time picker in Application Insights or by adding `| where timestamp > ago(90d)` to filter data
5. Results can be visualized as charts in Application Insights by selecting the appropriate visualization type

## Additional Resources

- [Kusto Query Language documentation](https://docs.microsoft.com/azure/data-explorer/kusto/query/)
- [Application Insights Analytics](https://docs.microsoft.com/azure/azure-monitor/app/analytics)
