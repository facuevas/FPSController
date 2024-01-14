using Godot;

namespace FPSController.Scripts.Util;

public static class PlayerInputMapUtil
{
    public static readonly StringName MoveForward = "move_forward";
    public static readonly StringName MoveBackward = "move_backward";
    public static readonly StringName MoveLeft = "move_left";
    public static readonly StringName MoveRight = "move_right";
    public static readonly StringName MoveJump = "move_jump";
    public static readonly StringName MoveCrouch = "move_crouch";
    public static readonly StringName MoveSprint = "move_sprint";
    public static readonly StringName FreeLook = "free_look";
}
