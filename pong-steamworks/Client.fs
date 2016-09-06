module Client

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open System.Collections.Generic
open System.IO
open Lidgren.Network

open HelperFunctions


//  SYSTEMS

//draw entities with position and appearance
//let RunAppearance textures (sb:SpriteBatch) dummyTexture world =
//    for entry in world.appearance do
//        let id = entry.Key
//        let appearance = entry.Value
//
//        let position = tryFind id world.position
//        let texture = tryFind appearance.texture textures
//
//        if Option.isNone position then
//            eprintfn "Client.RunAppearance: Cannot render entity \"%s\" without a position component!" id
//        else            
//            if Option.isNone texture then
//                eprintfn "Client.RunAppearance: Texture missing for \"%s\"!" id
//                sb.Draw(dummyTexture, Option.get position, Color.Magenta)
//            else
//                sb.Draw(Option.get texture, Option.get position, Color.White)


//Public facing functions
let StartSocket (ip:string) port = 
    let config = new NetPeerConfiguration("pong")
    let client = new NetClient(config)
    client.Start()
    ignore(client.Connect(ip, port))
    client

let RunFrame serverState (socket : NetClient) dt =
    let mutable message = socket.ReadMessage()

    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            printfn "Client: Incoming packet byte size: %i" message.LengthBytes

        | NetIncomingMessageType.StatusChanged -> ()
        | NetIncomingMessageType.DebugMessage -> ()
        | _ ->
            eprintfn "Client: Unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- socket.ReadMessage()