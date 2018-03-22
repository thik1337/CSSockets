﻿using System;
using System.Net;
using CSSockets.Streams;
using System.Net.Sockets;

namespace CSSockets.Tcp.Wrap
{
    public class SocketWrapper
    {
        public const bool SERVER_EXCLUSIVE = true;
        public const int SERVER_BACKLOG = 511;

        public Socket Socket { get; }
        internal IOThread BoundThread = null;
        public WrapperType Type { get; internal set; } = WrapperType.Unset;
        private WrapperState _state = WrapperState.Unset;
        public WrapperState State
        {
            get => _state;
            internal set => _state = value;
        }
        public IPEndPoint Local { get; internal set; } = null;
        public IPEndPoint Remote { get; internal set; } = null;

        public SocketWrapper() : this(new Socket(SocketType.Stream, ProtocolType.Tcp)) { }
        public SocketWrapper(Socket socket)
        {
            Socket = socket;
            Socket.Blocking = false;
            Socket.NoDelay = true;
        }

        public void WrapperBind()
            => (BoundThread = IOControl.GetBest()).Enqueue(new IOOperation()
            {
                Callee = this,
                Type = IOOperationType.WrapperBind,
                AdvanceTo = WrapperState.Dormant,
                FailAdvanceTo = WrapperState.Unset,
                BrokenAdvanceTo = WrapperState.Destroyed
            });
        public void WrapperAddClient(Connection connection)
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                User_1 = connection,
                Type = IOOperationType.WrapperAddClient,
                AdvanceTo = WrapperState.ClientDormant,
                FailAdvanceTo = WrapperState.Dormant,
                BrokenAdvanceTo = WrapperState.Destroyed
            });
        public void WrapperAddServer(Listener listener)
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                User_2 = listener,
                Type = IOOperationType.WrapperAddServer,
                AdvanceTo = WrapperState.ServerWaitBind,
                FailAdvanceTo = WrapperState.Dormant,
                BrokenAdvanceTo = WrapperState.Destroyed
            });

        public void ServerLookup(EndPoint endPoint)
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                Lookup = endPoint,
                Type = IOOperationType.ServerLookup,
                AdvanceTo = WrapperState.ServerBound,
                FailAdvanceTo = WrapperState.ServerWaitBind,
                BrokenAdvanceTo = WrapperState.Destroyed
            });
        public void ServerListen()
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                Type = IOOperationType.ServerListen,
                AdvanceTo = WrapperState.ServerListening,
                FailAdvanceTo = WrapperState.ServerBound,
                BrokenAdvanceTo = WrapperState.Destroyed
            });
        public void ServerTerminate()
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                Type = IOOperationType.ServerTerminate,
                AdvanceTo = WrapperState.Destroyed,
                FailAdvanceTo = WrapperState.ServerListening,
                BrokenAdvanceTo = WrapperState.Destroyed
            });

        public void ClientConnect(EndPoint endPoint)
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                Lookup = endPoint,
                Type = IOOperationType.ClientConnect,
                AdvanceTo = WrapperState.ClientConnecting,
                FailAdvanceTo = WrapperState.ClientClosed,
                BrokenAdvanceTo = WrapperState.Destroyed
            });
        public void ClientShutdown()
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                Type = IOOperationType.ClientShutdown,
                FailAdvanceTo = WrapperState.ClientClosed,
                BrokenAdvanceTo = WrapperState.Destroyed
            });
        public void ClientTerminate()
            => BoundThread.Enqueue(new IOOperation()
            {
                Callee = this,
                Type = IOOperationType.ClientTerminate,
                FailAdvanceTo = WrapperState.ClientClosed,
                BrokenAdvanceTo = WrapperState.Destroyed
            });

        public SocketErrorHandler WrapperOnSocketError { get; set; }
        public ControlHandler WrapperOnUnbind { get; set; }

        public ConnectionHandler ServerOnConnection { get; set; }

        public ControlHandler ClientOnConnect { get; set; }
        public ControlHandler ClientOnTimeout { get; set; }
        public ControlHandler ClientOnRecvShutdown { get; set; }
        public ControlHandler ClientOnClose { get; set; }

        public int ServerBacklog { get; set; } = SERVER_BACKLOG;
        public bool ServerExclusive { get; set; } = SERVER_EXCLUSIVE;

        public bool ClientAllowHalfOpen { get; set; } = false;
        public DateTime ClientLastActivity { get; internal set; } = DateTime.UtcNow;
        public TimeSpan? ClientTimeoutAfter { get; set; } = null;
        public bool ClientCalledTimeout { get; internal set; } = false;
    }
}
