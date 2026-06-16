namespace MyGame.Gameplay.Components;

public struct ProjectileTag { }

public struct Lifetime
{
    public float Remaining;
}

public struct Damage
{
    public int Amount;
}

public struct ProjectileSpawnRequest
{
    public float StartX;
    public float StartY;
    public float VelocityX;
    public float VelocityY;
}
