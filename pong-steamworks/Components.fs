module Components

open Microsoft.Xna.Framework

//  Component data
type Position = Vector2
let defaultPosition = Vector2(0.f, 0.f)

type Velocity = Vector2
let defaultVelocity = Vector2(0.f, 0.f)

type Appearance = { texture : string; size : Vector2 }
let defaultAppearance = { texture = ""; size = Vector2(0.f, 0.f);}

type Component =
    | Position of Position
    | Velocity of Velocity
    | Appearance of Appearance

type ComponentType =
    | Position
    | Velocity
    | Appearance

let ComponentToComponentType x =
    match x with
        | Component.Position _ -> ComponentType.Position
        | Component.Velocity _ -> ComponentType.Velocity
        | Component.Appearance _ -> ComponentType.Appearance

//ComponentBit is used for sending schema info over the internet
type ComponentBit =
    | Position = 0x00000001
    | Velocity = 0x00000002
    | Appearance = 0x00000004
