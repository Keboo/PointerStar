export interface Role {
  id: string
  name: string
}

export interface User {
  id: string
  name: string
  originalVote?: string | null
  role: Role
  vote?: string | null
}

export interface RoomState {
  autoShowVotes: boolean
  resetVotesRequestedAt?: string | null
  resetVotesRequestedBy?: string | null
  roomId: string
  users: User[]
  voteOptions: string[]
  voteStartTime?: string | null
  votesShown: boolean
}

export interface RoomOptions {
  autoShowVotes?: boolean
  voteOptions?: string[]
  votesShown?: boolean
}

export interface UserOptions {
  name?: string | null
  role?: Role | null
}

export interface RecentRoom {
  lastAccessed: string
  roomId: string
}

export interface ClientConfig {
  applicationInsightsConnectionString?: string | null
  appVersion?: string | null
}

export const roles = {
  facilitator: {
    id: '5fea7d71-fb62-405c-823c-09752c684bf0',
    name: 'Facilitator',
  },
  teamMember: {
    id: '116b133b-b16d-4a92-a3ce-ae53688e973c',
    name: 'Team Member',
  },
  observer: {
    id: 'a0fec1ad-caee-4fa0-8d93-d0ce970f92d7',
    name: 'Observer',
  },
} as const satisfies Record<string, Role>

export function roleFromId(roleId?: string | null): Role | null {
  if (!roleId) {
    return null
  }

  if (roleId === roles.facilitator.id) {
    return roles.facilitator
  }

  if (roleId === roles.teamMember.id) {
    return roles.teamMember
  }

  if (roleId === roles.observer.id) {
    return roles.observer
  }

  return null
}

export const roomHubMethods = {
  cancelResetVotes: 'CancelResetVotes',
  getServerTime: 'GetServerTime',
  joinRoom: 'JoinRoom',
  removeUser: 'RemoveUser',
  requestResetVotes: 'RequestResetVotes',
  resetVotes: 'ResetVotes',
  roomUpdated: 'RoomUpdated',
  submitVote: 'SubmitVote',
  updateRoom: 'UpdateRoom',
  updateUser: 'UpdateUser',
} as const

export const votingPresets = [
  {
    name: 'Fibonacci (1-21)',
    options: ['1', '2', '3', '5', '8', '13', '21', 'Abstain', '?'],
  },
  {
    name: 'Linear (1-5)',
    options: ['1', '2', '3', '4', '5', 'Abstain', '?'],
  },
  {
    name: 'Linear (1-10)',
    options: ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', 'Abstain', '?'],
  },
  {
    name: 'T-Shirt Sizes',
    options: ['XS', 'S', 'M', 'L', 'XL', 'XXL', 'Abstain', '?'],
  },
] as const

export const defaultVoteOptions = [...votingPresets[0].options]
export const userNameMaxLength = 40
