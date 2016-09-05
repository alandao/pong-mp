module Server

open Lidgren.Network

open ECS
open ECSTypes
open NetBuffer

let RunTick serverState (socket : NetServer) dt =
    //process messages from clients
    let mutable message = socket.ReadMessage()
    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            let data = message.ReadString()
            printfn "Server: message '%s' received" data
        | NetIncomingMessageType.StatusChanged ->
            //A client connected or disconnected.
            match message.SenderConnection.Status with
            | NetConnectionStatus.Connected -> //A player has joined
                serverState.clients.Add(message.SenderConnection, [])
                printfn "Server: Client from %s has connected." (message.SenderEndPoint.ToString())
            | NetConnectionStatus.Disconnected ->
                serverState.clients.Remove(message.SenderConnection) |> ignore
                printfn "Server: Client from %s has disconnected." (message.SenderEndPoint.ToString())
            | _ ->
                printfn "Server: Client from %s has an unhandled status." (message.SenderEndPoint.ToString())
        | NetIncomingMessageType.DebugMessage ->
            printfn "%s" (message.ReadString())
        | _ ->
            eprintfn "Server: Unhandled message with type: %s" (message.MessageType.ToString())
            ()           
        message <- socket.ReadMessage()

    //simulate systems

    //send snapshots to clients.
    for x in serverState.clients.Keys do
        let snapshots = serverState.clients.[x]

        let deltaSnapshot = DeltaSnapshot snapshots serverState.entityManager

        let sendMsg = socket.CreateMessage()
        sendMsg.Write(NetBufferSnapshot deltaSnapshot)
        socket.SendMessage(sendMsg, x, NetDeliveryMethod.Unreliable) |> ignore

        //update snapshot circular buffer with new snapshot.
        let updatedSnapshots =
            if List.length snapshots > snapshotBufferSize then
                snapshots.Tail @ [deltaSnapshot]
            else
                snapshots @ [deltaSnapshot]
        serverState.clients.[x] <- updatedSnapshots

let StartSocket port =
    let config = new NetPeerConfiguration("pong")
    config.Port <- port
    let server = new NetServer(config)
    server.Start()
    server
