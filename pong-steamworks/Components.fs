module Components

open Microsoft.Xna.Framework

//  Components
type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

type Appearance = { texture : string; size : Vector2 }
let defaultAppearance = { texture = ""; size = Vector2(0.f, 0.f);}

//ComponentBit is used for sending schema info over the internet
type ComponentBit =
    | Position = 0x00000001
    | Velocity = 0x00000002
    | Appearance = 0x00000004
