module SharedServerClient

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input


type Entity = string

//  Components
type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

type Appearance = { texture : string; size : Vector2 }
let defaultAppearance = { texture = ""; size = Vector2(0.f, 0.f);}

type Component = 
    | CPosition of Position
    | CVelocity of Vector2
    | CAppearance of Appearance

type ComponentBit =
    | bPosition = 0x00000001
    | bVelocity = 0x00000002
    | bAppearance = 0x00000004

type PlayerInput = 
    | PaddleUp of bool
    | PaddleDown of bool

type ClientToServerMsg = 
    | PaddleAt of x : float * y : float


//  Server Message types
let msg_snapshot = 0

