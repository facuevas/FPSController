using FPSController.Scripts.Util;
using Godot;

namespace FPSController.Scripts.Player;

public partial class Player : CharacterBody3D
{
    [Export]
    public float CurrentSpeed { get; set; } = 5.0f;

    [Export]
    public float JumpVelocity { get; set; } = 6.0f;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    // Movement
    private Vector3 _direction = Vector3.Zero;
    private const float WalkingSpeed = 5.0f;
    private const float SprintingSpeed = 8.0f;
    private const float CrouchingSpeed = 3.0f;
    private const float LerpSpeed = 10.0f;

    // Player state
    private bool _isWalking;
    private bool _isSprinting;
    private bool _isCrouching;
    private bool _isFreeLooking;
    private bool _isSliding;

    // Mouse Sensitivity
    // We will refactor this out eventually
    [Export]
    public float MouseSensitivity { get; set; } = 0.1f;

    // Node references
    private Node3D _neck;
    private Node3D _head;
    private CollisionShape3D _standingCollisionShape;
    private CollisionShape3D _crouchingCollisionShape;
    private RayCast3D _rayCast;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _neck = GetNode<Node3D>("Neck");
        _head = GetNode<Node3D>("Neck/Head");
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

            // Free look
            if (_isFreeLooking)
            {
                _neck.RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));
                _neck.Rotation = new Vector3(
                    0f,
                    Mathf.Clamp(_neck.Rotation.Y, Mathf.DegToRad(-120f), Mathf.DegToRad(120f)),
                    0f
                );
            }
            else
            {
                // Non-free look
                RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));
                _head.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
                _head.Rotation = new Vector3(Mathf.Clamp(_head.Rotation.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f)),
                    0f,
                    0f
                );
            }
        }
    }

    public override void _Process(double delta)
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        PlayerFreeLook(delta);
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
            _head.Position = _head.Position.Lerp(new Vector3(0f, -0.5f, 0f), LerpSpeed * (float)delta);
            _standingCollisionShape.Disabled = true;
            _crouchingCollisionShape.Disabled = false;
            _isWalking = false;
            _isSprinting = false;
            _isCrouching = true;
            CurrentSpeed = GetPlayerSpeed();
        }
        else if (!_rayCast.IsColliding())
        {
            _head.Position = _head.Position.Lerp(new Vector3(0f, 0f, 0f), LerpSpeed * (float)delta);
            _standingCollisionShape.Disabled = false;
            _crouchingCollisionShape.Disabled = true;
            _isCrouching = false;
            _isSprinting = Input.IsActionPressed(PlayerInputMapUtil.MoveSprint);
            _isWalking = !_isSprinting;
            CurrentSpeed = GetPlayerSpeed();
        }
    }

    private void PlayerFreeLook(double delta)
    {
        _isFreeLooking = Input.IsActionPressed(PlayerInputMapUtil.FreeLook);
        if (!_isFreeLooking)
        {
            // Reset the neck if we are no longer free looking.
            //_neck.Rotation.Lerp(Vector3.Zero, (float)delta * LerpSpeed);
            var neckLerp = Mathf.Lerp(_neck.Rotation.Y, 0.0f, LerpSpeed * (float)delta);
            _neck.Rotation = new Vector3(0f, neckLerp, 0f);
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