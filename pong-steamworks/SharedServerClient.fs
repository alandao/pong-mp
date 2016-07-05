module SharedServerClient

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open System.Collections.Generic


type Entity = string

//  Components
type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

type Appearance = { texture : string; size : Vector2 }
let defaultAppearance = { texture = ""; size = Vector2(0.f, 0.f);}

//ComponentBit is used for concisely sending schema info over the internet
type ComponentBit =
    | Position = 0x00000001
    | Velocity = 0x00000002
    | Appearance = 0x00000004

type EntityComponentDictionary =
    | EntityPosition of Dictionary<Entity, Position>
    | EntityVelocity of Dictionary<Entity, Velocity>
    | EntityAppearance of Dictionary<Entity, Appearance>
let EntityComponentRemove id dict =
    match dict with
    | EntityPosition dict -> dict.Remove(id)
    | EntityVelocity dict -> dict.Remove(id)
    | EntityAppearance dict -> dict.Remove(id)


type PlayerInput = 
    | PaddleUp of bool
    | PaddleDown of bool

type ClientToServerMsg = 
    | PaddleAt of x : float * y : float


//  Server Message types
let msg_snapshot = 0

