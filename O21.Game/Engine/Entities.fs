// SPDX-FileCopyrightText: 2024 O21 contributors <https://github.com/ForNeVeR/O21>
//
// SPDX-License-Identifier: MIT

namespace O21.Game.Engine

open O21.Game.U95
open O21.Game.Engine.Geometry

type Player = {
    TopLeft: Point
    Velocity: Vector
    Direction: HorizontalDirection
    ShotCooldown: int
    FreezeTime: int
    Lives: int
    Oxygen: OxygenStorage
} with

    member this.TopRight = this.TopLeft + Vector(GameRules.PlayerSize.X, 0)

    member this.IsAllowedToShoot = this.ShotCooldown = 0
    member this.IsAllowedToMove = this.FreezeTime = 0
    
    member this.OxygenAmount = this.Oxygen.Amount

    /// The coordinate of top corner of the forward side (i.e. the one it's directed at) of the sprite.
    member this.TopForward =
        match this.Direction with
        | HorizontalDirection.Left -> this.TopLeft
        | HorizontalDirection.Right -> this.TopRight
        
    member this.Box: Box = { TopLeft = this.TopLeft; Size = GameRules.PlayerSize }

    member this.Update(level: Level, timeDelta: int): PlayerEffect =
        let newPlayer =
            { this with
                TopLeft = this.TopLeft + this.Velocity * timeDelta
                ShotCooldown = max (this.ShotCooldown - timeDelta) 0
                FreezeTime =  max (this.FreezeTime - timeDelta) 0
                Oxygen = this.Oxygen.Update(timeDelta)
            }
        newPlayer.CheckState(level)
        
    member private this.CheckState(level: Level) =
        if this.Oxygen.IsEmpty then PlayerEffect.Die
        else
            match CheckCollision level this.Box with
            | Collision.OutOfBounds -> PlayerEffect.Update this // TODO[#28]: Level progression
            | Collision.CollidesBrick -> PlayerEffect.Die
            | Collision.None -> PlayerEffect.Update this
        
    static member Default = {
            TopLeft = GameRules.PlayerStartingPosition
            Velocity = Vector(0, 0)
            ShotCooldown = 0
            FreezeTime = 0 
            Direction = Right
            Lives = GameRules.InitialPlayerLives
            Oxygen = OxygenStorage.Default
        }
and OxygenStorage = {
    Amount: int
    Timer: GameTimer
} with
    
    member this.IsEmpty = this.Amount < 0
    
    member this.Update(timeDelta: int) =
        let timer = this.Timer.Update(timeDelta)
        if timer.HasExpired then
            let newAmount =
                if not this.IsEmpty then
                    this.Amount - timer.ExpirationCount
                else GameRules.MaxOxygenUnits
            {                
                Amount = newAmount
                Timer = timer.Reset
            }
        else
            { this with
                Timer = timer 
            }
    
    static member Default = {
        Amount = GameRules.MaxOxygenUnits
        Timer = { GameTimer.Default with Period = GameRules.OxygenUnitPeriod }
    }

and [<RequireQualifiedAccess>] PlayerEffect =
    | Update of Player
    | Die

type Bullet = {
    TopLeft: Point
    Direction: HorizontalDirection
    Lifetime: int
    Velocity: Vector
} with
    member this.Box = { TopLeft = this.TopLeft; Size = GameRules.BulletSize }
    
    member this.Update(level: Level, timeDelta: int): Bullet option =
        // Check each intermediate position of the bullet for collision:
        let maxTimeToProcessInOneStep = GameRules.BrickSize.X / this.Velocity.X
        if maxTimeToProcessInOneStep <= 0 then failwith "maxTimeToProcessInOneStep <= 0"
        
        let newLifetime = this.Lifetime + timeDelta

        if timeDelta <= maxTimeToProcessInOneStep then
            let newTopLeft =
                this.TopLeft +
                Vector(this.Direction * this.Velocity.X * timeDelta, this.Velocity.Y * timeDelta)
            let newBullet = { this with TopLeft = newTopLeft; Lifetime = newLifetime }
            
            if newLifetime > GameRules.BulletLifetime then None
            else
                match CheckCollision level newBullet.Box with
                | Collision.OutOfBounds -> None
                | Collision.CollidesBrick -> None
                | Collision.None -> Some newBullet
        else
            this.Update(level, maxTimeToProcessInOneStep)
            |> Option.bind _.Update(level, timeDelta - maxTimeToProcessInOneStep)

type Particle = {
    TopLeft: Point
    Speed: int
} with
    member this.Box = { TopLeft = this.TopLeft; Size = GameRules.ParticleSize }
    
    member this.Update(level: Level, timeDelta: int): Particle option =
        let newPosition =
            this.TopLeft +
            Vector(0, VerticalDirection.Up * this.Speed * timeDelta)
        let newParticle = { this with TopLeft = newPosition }
        match CheckCollision level newParticle.Box with
        | Collision.OutOfBounds -> None
        | Collision.CollidesBrick -> None
        | Collision.None -> Some newParticle
