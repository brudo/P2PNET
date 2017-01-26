﻿using Sockets.Plugin;
using System;
using System.Collections.Generic;
using P2PNET.TransportLayer.EventArgs;
using System.Threading.Tasks;

namespace P2PNET.TransportLayer
{
    /// <summary>
    /// Low level class that sends and receives messages between peers.
    /// </summary>
    public class TransportManager
    {
        /// <summary>
        /// Triggered when a new peer is detected or an existing peer becomes inactive
        /// </summary>
        public event EventHandler<PeerChangeEventArgs> PeerChange;

        /// <summary>
        /// Triggered when a message has been received by this peer
        /// </summary>
        public event EventHandler<MsgReceivedEventArgs> MsgReceived;

        /// <summary>
        /// A list of all peers that are known to this peer
        /// </summary>
        public List<Peer> KnownPeers
        {
            get
            {
                return this.baseStation.KnownPeers;
            }
        }
        private string ipAddress = null;

        /// <summary>
        /// Get methods that retreives the IPv4 address of the local peer asynchronously
        /// </summary>
        /// <returns> A string in the format xxxx.xxxx.xxxx.xxxx  </returns>
        public async Task<string> GetIpAddress()
        {
            if(ipAddress == null)
            {
                ipAddress = await GetLocalIPAddress();
            }
            return ipAddress;
        }

        /// <summary>
        /// The port number used for sending and receiving messages
        /// </summary>
        public int PortNum { get; }


        private Listener listener;
        private BaseStation baseStation;

        /// Constructor that instantiates a transport manager. To commence listening call the method <C>StartAsync</C>.
        /// </summary>
        /// <param name="mPortNum"> The port number which this peer will listen on and send messages with </param>
        /// <param name="mForwardAll"> When true, all messages received trigger a MsgReceived event. This includes UDB broadcasts that are reflected back to the local peer.</param>
        public TransportManager(int mPortNum = 8080, bool mForwardAll = false)
        {

            this.PortNum = mPortNum;
            this.listener = new Listener(this.PortNum);
            this.baseStation = new BaseStation(this.PortNum, mForwardAll);

            this.baseStation.PeerChange += BaseStation_PeerChange;
            this.baseStation.MsgReceived += IncomingMsg;

            //baseStation looks up incoming messages to see if there is a new peer talk to us
            this.listener.IncomingMsg += baseStation.IncomingMsgAsync;
            this.listener.PeerConnectTCPRequest += baseStation.NewTCPConnection;
        }

        //deconstructor
        ~TransportManager()
        {
            this.StopAsync().Wait();
        }

        /// <summary>
        /// Peer will start actively listening on the specified port number.
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            baseStation.LocalIpAddress = await GetLocalIPAddress();
            await listener.StartAsync();
        }

        /// <summary>
        /// Peer will stop listening and will close all establish connections. All known peers will be cleared.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            await listener.StopAsync();

            //clear known peers
            KnownPeers.Clear();
        }

        /// <summary>
        /// Sends a message to a peer via a reliable TCP connection
        /// </summary>
        /// <param name="ipAddress"> the IPv4 address to send the message to. In the format "xxxx.xxxx.xxxx.xxxx" </param>
        /// <param name="msg">The message to send to the other peer</param>
        /// <returns>true if message was sucessfully sent otherwise returns false</returns>
        public async Task<bool> SendAsyncTCP(string ipAddress, byte[] msg)
        {
            return await baseStation.SendTCPMsgAsync(ipAddress, msg);
        }

        /// <summary>
        /// Sends a message to a peer via an unreliable UDP connection.
        /// Use <C>SendAsyncTCP</C> instead if packet loss cannot be tolerated. 
        /// </summary>
        /// <param name="ipAddress"> the IPv4 address to send the message to. In the format "xxxx.xxxx.xxxx.xxxx" </param>
        /// <param name="msg">The message to send to the other peer</param>
        /// <returns>true if message was sucessfully sent otherwise returns false</returns>
        public async Task<bool> SendAsyncUDP(string ipAddress, byte[] msg)
        {
            return await baseStation.SendUDPMsgAsync(ipAddress, msg);
        }

        /// <summary>
        /// Sends an unreliable UDP broadcast to the local router. Depending on your local router settings UDP broadcasts may be ignored.
        /// If the address of other peers is known use <C>SendToAllPeersAsyncUDP</C> instead.
        /// </summary>
        /// <param name="msg">The message broadcast to other peers</param>
        /// <returns></returns>
        public async Task SendBroadcastAsyncUDP(byte[] msg)
        {
            await baseStation.SendUDPBroadcastAsync(msg);
        }

        /// <summary>
        /// Sends a message via unreliable UDP to all known peers.
        /// Use <C>SendToAllPeersAsyncTCP</C> instead if packet loss can not be tolerated. 
        /// </summary>
        /// <param name="msg">The message sent to all other peers</param>
        /// <returns></returns>
        public async Task SendToAllPeersAsyncUDP(byte[] msg)
        {
            await baseStation.SendTCPMsgToAllUDPAsync(msg);
        }

        /// <summary>
        /// Sends a message via reliable TCP connections to all known peers.
        /// </summary>
        /// <param name="msg">The message sent to all other peers</param>
        /// <returns></returns>
        public async Task SendToAllPeersAsyncTCP(byte[] msg)
        {
            await baseStation.SendTCPMsgToAllTCPAsync(msg);
        }

        //This is here for existing Peer to Peer systems that use asynchronous Connections.
        //This method is not needed otherwise because this class automatically keeps track
        //of peer connections
        /// <summary>
        /// This method is avaliable to make it easier to integrate existing asymetric peer to peer systems.
        /// </summary>
        /// <param name="ipAddress">the ip address to establish a connection with</param>
        /// <returns></returns>
        public async Task DirrectConnectAsyncTCP(string ipAddress)
        {
            await baseStation.DirectConnectTCPAsync(ipAddress);
        }

        private void IncomingMsg(object sender, MsgReceivedEventArgs e)
        {
            //send message out
            MsgReceived?.Invoke(this, e);
        }

        private void BaseStation_PeerChange(object sender, PeerChangeEventArgs e)
        {
            PeerChange?.Invoke(this, e);
        }

        private async Task<string> GetLocalIPAddress()
        {
            List<CommsInterface> interfaces = await CommsInterface.GetAllInterfacesAsync();
            foreach(CommsInterface comms in interfaces)
            {
                if(comms.ConnectionStatus == Sockets.Plugin.Abstractions.CommsInterfaceStatus.Connected)
                {
                    return comms.IpAddress;
                }
            }

            //raise exception
            throw (new NoNetworkInterface("Unable to find an active network interface connection. Is this device connected to wifi?"));
        }
    }
}