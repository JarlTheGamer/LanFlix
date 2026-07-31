/**
 * SyncPlay Client Module
 * Manages SignalR WebSocket connection to /hubs/syncplay for Watch Party synchronization
 */

import * as signalR from '@microsoft/signalr';

export class SyncPlayClient {
    constructor() {
        this.connection = null;
        this.currentRoom = null;
        this.listeners = new Map();
        this.pingIntervalId = null;
        this.rttMs = 0;
        this.serverTimeOffsetMs = 0;
    }

    /**
     * Connect to SyncPlay SignalR Hub
     */
    async connect() {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            return;
        }

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/syncplay')
            .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.registerServerEvents();

        try {
            await this.connection.start();
            console.log('Connected to SyncPlay Hub');
            this.startPingLoop();
        } catch (error) {
            console.error('Failed to connect to SyncPlay Hub:', error);
            throw error;
        }
    }

    /**
     * Register listeners for server events
     */
    registerServerEvents() {
        this.connection.on('RoomJoined', (room) => {
            this.currentRoom = room;
            this.emit('roomJoined', room);
        });

        this.connection.on('UserJoined', (participant, room) => {
            this.currentRoom = room;
            this.emit('userJoined', { participant, room });
        });

        this.connection.on('UserLeft', (participant, room) => {
            this.currentRoom = room;
            this.emit('userLeft', { participant, room });
        });

        this.connection.on('RoomClosed', () => {
            this.currentRoom = null;
            this.emit('roomClosed');
        });

        this.connection.on('PlaybackStateSynced', (action, room) => {
            this.currentRoom = room;
            this.emit('playbackStateSynced', { action, room });
        });

        this.connection.on('ChatMessageReceived', (chatMessage) => {
            this.emit('chatMessageReceived', chatMessage);
        });

        this.connection.on('EmojiReactionReceived', (emojiReaction) => {
            this.emit('emojiReactionReceived', emojiReaction);
        });

        this.connection.on('MediaChanged', (mediaChange, room) => {
            this.currentRoom = room;
            this.emit('mediaChanged', { mediaChange, room });
        });

        this.connection.on('Pong', (clientTimestamp, serverTimestamp) => {
            const now = Date.now();
            this.rttMs = Math.max(0, now - clientTimestamp);
            const estimatedServerTime = serverTimestamp + (this.rttMs / 2);
            this.serverTimeOffsetMs = estimatedServerTime - now;
        });

        this.connection.on('JoinFailed', (reason) => {
            this.emit('joinFailed', reason);
        });
    }

    /**
     * Start room creation
     */
    async createRoom(profileId, profileName, profileAvatar, contentId, contentType, episodeId = null) {
        await this.connect();
        return await this.connection.invoke('CreateRoom', profileId, profileName, profileAvatar || '', parseInt(contentId, 10), contentType, episodeId ? parseInt(episodeId, 10) : null);
    }

    /**
     * Join existing room by code
     */
    async joinRoom(roomCode, profileId, profileName, profileAvatar) {
        await this.connect();
        return await this.connection.invoke('JoinRoom', roomCode, profileId, profileName, profileAvatar || '');
    }

    /**
     * Leave active room
     */
    async leaveRoom() {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            await this.connection.invoke('LeaveRoom');
        }
        this.currentRoom = null;
    }

    /**
     * Send playback action (Play, Pause, Seek, RateChange)
     */
    async sendPlaybackAction(actionType, positionSeconds, isPlaying, playbackRate, profileId, profileName) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) return;
        await this.connection.invoke('SendPlaybackAction', actionType, positionSeconds, isPlaying, playbackRate, profileId, profileName);
    }

    /**
     * Send chat message
     */
    async sendChatMessage(message, profileId, profileName, profileAvatar) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) return;
        await this.connection.invoke('SendChatMessage', message, profileId, profileName, profileAvatar || '');
    }

    /**
     * Send quick emoji reaction
     */
    async sendEmojiReaction(emoji, profileId, profileName) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) return;
        await this.connection.invoke('SendEmojiReaction', emoji, profileId, profileName);
    }

    /**
     * Host changes the media for the entire room
     */
    async changeMedia(contentId, contentType, episodeId, mediaTitle) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) return;
        await this.connection.invoke('ChangeMedia', parseInt(contentId, 10), contentType, episodeId ? parseInt(episodeId, 10) : null, mediaTitle);
    }

    /**
     * Latency measurement loop
     */
    startPingLoop() {
        if (this.pingIntervalId) clearInterval(this.pingIntervalId);
        this.pingIntervalId = setInterval(async () => {
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                const videoEl = document.getElementById('video-player');
                const currentTime = videoEl ? videoEl.currentTime : 0;
                await this.connection.invoke('Ping', Date.now(), currentTime).catch(() => {});
            }
        }, 1500);
    }

    /**
     * Event listener registration helper
     */
    on(event, callback) {
        if (!this.listeners.has(event)) {
            this.listeners.set(event, []);
        }
        this.listeners.get(event).push(callback);
    }

    emit(event, data) {
        const callbacks = this.listeners.get(event);
        if (callbacks) {
            callbacks.forEach(cb => cb(data));
        }
    }

    /**
     * Check if current client is the host of the active room
     */
    isHost() {
        if (!this.currentRoom || !this.connection) return false;
        return this.currentRoom.hostConnectionId === this.connection.connectionId;
    }

    disconnect() {
        if (this.pingIntervalId) clearInterval(this.pingIntervalId);
        if (this.connection) {
            this.connection.stop();
        }
    }
}

// Global singleton instance
const syncPlayClient = new SyncPlayClient();
export default syncPlayClient;
