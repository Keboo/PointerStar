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
  private reconnectHandler?: () => Promise<void>

  public constructor(url = `${window.location.origin}/RoomHub`) {
    this.connection = new HubConnectionBuilder().withUrl(url).withAutomaticReconnect().build()

    this.connection.on(roomHubMethods.roomUpdated, (roomState: RoomState) => {
      this.roomStateListeners.forEach((listener) => listener(roomState))
    })

    // Re-join room after SignalR's built-in reconnect restores the transport.
    this.connection.onreconnected(() => {
      void this.reconnectHandler?.()
    })

    // Re-join room after all automatic reconnect attempts are exhausted.
    // The handler will call open() to start a new connection loop then rejoin.
    this.connection.onclose(() => {
      void this.reconnectHandler?.()
    })
  }

  public get isConnected() {
    return this.connection.state === HubConnectionState.Connected
  }

  /** True when the connection is fully stopped (not Connecting or Reconnecting). */
  public get isDisconnected() {
    return this.connection.state === HubConnectionState.Disconnected
  }

  /**
   * Registers a handler that is called whenever the connection is restored
   * (after automatic reconnect or after a cold re-open). The handler should
   * re-join the room using the last-known user identity.
   */
  public setReconnectHandler(handler: () => Promise<void>) {
    this.reconnectHandler = handler
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
