import {
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from '@microsoft/signalr'

import type { RoomOptions, RoomState, User, UserOptions } from '../types/contracts'
import { roomHubMethods } from '../types/contracts'
import { getJitteredDelayMs, waitForDelay } from './retry'

const hubRetryOptions = {
  baseDelayMs: 500,
  jitterRatio: 0.25,
  maxDelayMs: 10_000,
}

export class RoomHubClient {
  private readonly connection: HubConnection
  private readonly roomStateListeners = new Set<(roomState: RoomState) => void>()

  public constructor(url = `${window.location.origin}/RoomHub`) {
    this.connection = new HubConnectionBuilder().withUrl(url).withAutomaticReconnect().build()

    this.connection.on(roomHubMethods.roomUpdated, (roomState: RoomState) => {
      this.roomStateListeners.forEach((listener) => listener(roomState))
    })
  }

  public get isConnected() {
    return this.connection.state === HubConnectionState.Connected
  }

  public subscribe(listener: (roomState: RoomState) => void) {
    this.roomStateListeners.add(listener)

    return () => {
      this.roomStateListeners.delete(listener)
    }
  }

  public async open(signal?: AbortSignal) {
    let failedAttempts = 0

    while (!signal?.aborted) {
      if (
        this.connection.state === HubConnectionState.Connected ||
        this.connection.state === HubConnectionState.Connecting ||
        this.connection.state === HubConnectionState.Reconnecting
      ) {
        return
      }

      try {
        await this.connection.start()
        return
      } catch (error) {
        failedAttempts += 1
        const delayMs = getJitteredDelayMs(failedAttempts, hubRetryOptions)
        console.warn(`Unable to connect to the room hub. Retrying in ${delayMs}ms.`, error)
        await waitForDelay(delayMs, signal)
      }
    }
  }

  public async stop() {
    if (this.connection.state !== HubConnectionState.Disconnected) {
      await this.connection.stop()
    }
  }

  public joinRoom(roomId: string, user: User) {
    return this.connection.invoke(roomHubMethods.joinRoom, roomId, user)
  }

  public submitVote(vote: string) {
    return this.connection.invoke(roomHubMethods.submitVote, vote)
  }

  public updateRoom(options: RoomOptions) {
    return this.connection.invoke(roomHubMethods.updateRoom, options)
  }

  public updateUser(options: UserOptions) {
    return this.connection.invoke(roomHubMethods.updateUser, options)
  }

  public resetVotes() {
    return this.connection.invoke(roomHubMethods.resetVotes)
  }

  public requestResetVotes() {
    return this.connection.invoke(roomHubMethods.requestResetVotes)
  }

  public cancelResetVotes() {
    return this.connection.invoke(roomHubMethods.cancelResetVotes)
  }

  public removeUser(userId: string) {
    return this.connection.invoke(roomHubMethods.removeUser, userId)
  }

  public getServerTime() {
    return this.connection.invoke<string>(roomHubMethods.getServerTime)
  }
}
