module Client

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Content
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open System.Collections.Generic
open Lidgren.Network

open HelperFunctions
open SharedServerClient

//  COMPONENTS
type Appearance = { texture : Texture2D; size : Vector2 }


type World = {
    entities : HashSet<string>;

    position: Dictionary<string, Position>;
    velocity: Dictionary<string, Velocity>;
    appearance: Dictionary<string, Appearance>;
    }

let defaultWorld = {
    entities = HashSet<string>();
    position = Dictionary<string, Position>();
    velocity = Dictionary<string, Velocity>();
    appearance = Dictionary<string, Appearance>();
    }

let destroyEntity id world = 
    world.position.Remove(id) |> ignore
    world.velocity.Remove(id) |> ignore
    world.entities.Remove(id)

let addAppearance id textureName (contentManager:ContentManager) (world:World)  = 
    let appr =  {    
                    texture = contentManager.Load<Texture2D> textureName; 
                    size = Vector2(1.f, 1.f)
                }
    world.appearance.Add(id, appr)

//  SYSTEMS

//draw entities with position and appearance
let RunAppearance (sb:SpriteBatch) world =
    for entry in world.appearance do
        let id = entry.Key
        let appearance = entry.Value
        let position = tryFind id world.position

        if Option.isSome position then
            sb.Draw(appearance.texture, Option.get position, Color.White)


//  OTHER

let (getClientInputs : PlayerInput list) =
    let mutable inputs = List.empty
    if (Keyboard.GetState().IsKeyDown(Keys.W)) then
        inputs <- PaddleUp true :: inputs
    if (Keyboard.GetState().IsKeyDown(Keys.S)) then
        inputs <- PaddleDown true :: inputs
    inputs


//Public facing functions
let Start (ip:string) port = 
    let config = new NetPeerConfiguration("pong")
    let client = new NetClient(config)
    client.Start()
    ignore(client.Connect(ip, port))
    client

let Update (world:World) (clientSocket:NetClient) =
    let mutable message = clientSocket.ReadMessage()

    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            let data = message.ReadString()
            printfn "Client: Message received: %s" data

        | NetIncomingMessageType.StatusChanged -> ()
        | NetIncomingMessageType.DebugMessage -> ()
        | _ ->
            eprintfn "Client: Unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- clientSocket.ReadMessage()