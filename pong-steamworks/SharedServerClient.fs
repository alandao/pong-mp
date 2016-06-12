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

type PlayerInput = 
    | PaddleUp of bool
    | PaddleDown of bool

type ClientToServerMsg = 
    | PaddleAt of x : float * y : float


