using FPSController.Scripts.Util;
using Godot;

namespace FPSController.Scripts.Player;

public partial class Player : CharacterBody3D
{
    [Export]
    public float CurrentSpeed { get; set; } = 5.0f;

    [Export]
    public float JumpVelocity { get; set; } = 4.5f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    // Movement
    private Vector3 _direction = Vector3.Zero;
    private const float WalkingSpeed = 5.0f;
    private const float SprintingSpeed = 8.0f;
    private const float CrouchingSpeed = 3.0f;
    private const float LerpSpeed = 10.0f;
    private const float StandingHeight = 1.8f;
    private const float CrouchingHeight = 1.3f;
    private bool _isCrouching;
    private bool _isSprinting;

    // Mouse Sensitivity
    // We will refactor this out eventually
    [Export]
    public float MouseSensitivity { get; set; } = 0.1f;

    // Node references
    private Node3D _head;
    private CollisionShape3D _standingCollisionShape;
    private CollisionShape3D _crouchingCollisionShape;
    private RayCast3D _rayCast;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _head = GetNode<Node3D>("Head");
        _standingCollisionShape = GetNode<CollisionShape3D>("StandingCollisionShape");
        _crouchingCollisionShape = GetNode<CollisionShape3D>("CrouchingCollisionShape");
        _rayCast = GetNode<RayCast3D>("RayCast3D");
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;

        if (@event is InputEventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            // Mouse Movement
            var mouseMotion = (InputEventMouseMotion)@event;
            RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));
            _head.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
            _head.Rotation = new Vector3(Mathf.Clamp(_head.Rotation.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f)), 0f,
                0f);
        }
    }

    public override void _Process(double delta)
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        PlayerCrouch(delta);
        var velocity = PlayerMove(delta);
        Velocity = velocity;
        MoveAndSlide();
    }

    private Vector3 PlayerMove(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
        {
            velocity.Y -= gravity * (float)delta;
        }

        // Handle Jump.
        if (Input.IsActionJustPressed(PlayerInputMapUtil.MoveJump) && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Get the input direction and handle the movement/deceleration.
        // As good practice, you should replace UI actions with custom gameplay actions.
        Vector2 inputDir = Input.GetVector(
            PlayerInputMapUtil.MoveLeft,
            PlayerInputMapUtil.MoveRight,
            PlayerInputMapUtil.MoveForward,
            PlayerInputMapUtil.MoveBackward
        );

        // _direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        _direction = _direction.Lerp(
            (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized(),
            LerpSpeed * (float)delta
        );

        if (_direction != Vector3.Zero)
        {
            velocity.X = _direction.X * CurrentSpeed;
            velocity.Z = _direction.Z * CurrentSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, CurrentSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, CurrentSpeed);
        }

        return velocity;
    }

    private void PlayerCrouch(double delta)
    {
        // Player Crouch or Stand logic
        if (Input.IsActionPressed(PlayerInputMapUtil.MoveCrouch))
        {
            _head.Position = _head.Position.Lerp(new Vector3(0f, CrouchingHeight, 0f), LerpSpeed * (float)delta);
            _standingCollisionShape.Disabled = true;
            _crouchingCollisionShape.Disabled = false;
            _isCrouching = true;
            _isSprinting = false;
            CurrentSpeed = GetPlayerSpeed();
        }
        else if (!_rayCast.IsColliding())
        {
            _head.Position = _head.Position.Lerp(new Vector3(0f, StandingHeight, 0f), LerpSpeed * (float)delta);
            _standingCollisionShape.Disabled = false;
            _crouchingCollisionShape.Disabled = true;
            _isCrouching = false;
            _isSprinting = Input.IsActionPressed(PlayerInputMapUtil.MoveSprint);
            CurrentSpeed = GetPlayerSpeed();
        }
    }

    private float GetPlayerSpeed()
    {
        var lastSpeed = CurrentSpeed;
        if (!IsOnFloor())
        {
            return lastSpeed;
        }

        if (_isCrouching)
        {
            return CrouchingSpeed;
        }
        else if (_isSprinting)
        {
            return SprintingSpeed;
        }
        
        return WalkingSpeed;
    }
}