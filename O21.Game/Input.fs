namespace O21.Game

open System.Numerics
open Raylib_CsLo

type Key = Left | Right | Up | Down | Fire

type Input = {
    Pressed: Set<Key>
    MouseCoords: Vector2
    MouseButtonPressed: bool
    MouseWheelMove: float32
}

module Input =
    let keyBindings =
        Map.ofList
            [ 
                KeyboardKey.KEY_UP, Up
                KeyboardKey.KEY_DOWN, Down
                KeyboardKey.KEY_LEFT, Left
                KeyboardKey.KEY_RIGHT, Right
                KeyboardKey.KEY_SPACE, Fire 
            ]

type Input with    
    static member Handle(): Input =
        let keys = ResizeArray()
        let mutable key = Raylib.GetKeyPressed_()
        while key <> KeyboardKey.KEY_NULL do
            keys.Add(key)
            key <- Raylib.GetKeyPressed_()

        let mouse = Raylib.GetMousePosition()

        { Pressed = keys |> Seq.choose (fun k -> Map.tryFind k Input.keyBindings) |> Set.ofSeq
          MouseCoords = Vector2(mouse.X, mouse.Y)
          MouseButtonPressed = Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)
          MouseWheelMove = Raylib.GetMouseWheelMove() }
