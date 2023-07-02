using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public PlayerData Data;
	[SerializeField] private Animator anim;
	#region COMPONENTS
	public Rigidbody2D RB { get; private set; }
	//public PlayerAnimator AnimHandler { get; private set; }
	#endregion

	#region STATE PARAMETERS
	public bool IsFacingRight { get; private set; }
	public bool IsJumping { get; private set; }
	public bool IsWallJumping { get; private set; }
	public bool IsDashing { get; private set; }
	public bool IsSliding { get; private set; }
	public float LastOnGroundTime { get; private set; }
	public float LastOnWallTime { get; private set; }
	public float LastOnWallRightTime { get; private set; }
	public float LastOnWallLeftTime { get; private set; }

	

	//Jump
	private bool _isJumpCut;
	private bool _isJumpFalling;
	private bool _isGrounded;

	//Wall Jump
	private float _wallJumpStartTime;
	private int _lastWallJumpDir;

	//Dash
	private int _dashesLeft;
	private bool _dashRefilling;
	private Vector2 _lastDashDir;
	private bool _isDashAttacking;

	#endregion

	#region INPUT PARAMETERS
	private Vector2 _moveInput;
	public float LastPressedJumpTime { get; private set; }
	public float LastPressedDashTime { get; private set; }
	#endregion

	#region CHECK PARAMETERS
	[Header("Checks")]
	[SerializeField] private Transform _groundCheckPoint;
	[SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
	[SerializeField] private Transform _frontWallCheckPoint;
	[SerializeField] private Transform _backWallCheckPoint;
	[SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
	#endregion

	#region LAYERS & TAGS
	[Header("Layers & Tags")]
	[SerializeField] private LayerMask _groundLayer;
	#endregion

	private void Awake()
	{
		RB = GetComponent<Rigidbody2D>();
		
	}

	private void Start()
	{
		anim = GetComponent<Animator>();
		SetGravityScale(Data.gravityScale);
		IsFacingRight = true;
		_isGrounded = true;
	}

	private void Update()
	{
		
		#region TIMERS
		LastOnGroundTime -= Time.deltaTime;
		LastOnWallTime -= Time.deltaTime;
		LastOnWallRightTime -= Time.deltaTime;
		LastOnWallLeftTime -= Time.deltaTime;

		LastPressedJumpTime -= Time.deltaTime;
		LastPressedDashTime -= Time.deltaTime;
		#endregion

		#region INPUT HANDLER
		_moveInput.x = Input.GetAxisRaw("Horizontal");
		_moveInput.y = Input.GetAxisRaw("Vertical");

		if (_moveInput.x != 0)
			CheckDirectionToFace(_moveInput.x > 0);

		if (Input.GetKeyDown(KeyCode.W))
		{
			OnJumpInput();

		}

		if (Input.GetKeyUp(KeyCode.W))
		{
			OnJumpUpInput();

		}

		if (Input.GetKeyDown(KeyCode.LeftShift))
		{
			OnDashInput();
		}
		#endregion

		#region COLLISION CHECKS
		if (!IsDashing && !IsJumping)
		{
			//Ground Check
			if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer)) //checks if set box overlaps with ground
			{

				_isGrounded = true;
				LastOnGroundTime = Data.coyoteTime; //if so sets the lastGrounded to coyoteTime
			}

			//Right Wall Check
			if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && IsFacingRight)
					|| (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !IsFacingRight)) && !IsWallJumping)
				LastOnWallRightTime = Data.coyoteTime;

			//Right Wall Check
			if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !IsFacingRight)
				|| (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && IsFacingRight)) && !IsWallJumping)
				LastOnWallLeftTime = Data.coyoteTime;

			//Two checks needed for both left and right walls since whenever the play turns the wall checkPoints swap sides
			LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
		}
		#endregion

		#region JUMP CHECKS
		if (IsJumping && RB.velocity.y < 0)
		{
			IsJumping = false;

			_isJumpFalling = true;

		}

		if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
		{
			IsWallJumping = false;
		}

		if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping)
		{
			_isJumpCut = false;

			_isJumpFalling = false;
		}

		if (!IsDashing)
		{
			//Jump
			if (CanJump() && LastPressedJumpTime > 0 && _isGrounded)
			{
				IsJumping = true;
				IsWallJumping = false;
				_isJumpCut = false;
				_isJumpFalling = false;
				Jump();


				//jump anim
			}
			//WALL JUMP
			else if (CanWallJump() && LastPressedJumpTime > 0)
			{
				IsWallJumping = true;
				IsJumping = false;
				_isJumpCut = false;
				_isJumpFalling = false;

				_wallJumpStartTime = Time.time;
				_lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

				WallJump(_lastWallJumpDir);
				//wall jump anim
			}
		}
		#endregion

		#region DASH CHECKS
		if (CanDash() && LastPressedDashTime > 0)
		{
			//Freeze game for a split second.
			Sleep(Data.dashSleepTime);

			//Dash direction
			if (_moveInput != Vector2.zero)
				_lastDashDir = _moveInput;
			else
				_lastDashDir = IsFacingRight ? Vector2.right : Vector2.left;



			IsDashing = true;
			IsJumping = false;
			IsWallJumping = false;
			_isJumpCut = false;

			StartCoroutine(nameof(StartDash), _lastDashDir);
			//dash anim
		}
		#endregion

		#region IS SLIDE PERFORMABLE CHECK
		if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
			IsSliding = true;
		else
			IsSliding = false;
		#endregion

		#region GRAVITY ON DIFFERENT OCCATIONS
		if (!_isDashAttacking)
		{
			//Higher gravity if we've released the jump input or are falling
			if (IsSliding)
			{
				SetGravityScale(0);
			}
			else if (RB.velocity.y < 0 && _moveInput.y < 0)
			{
				//higher gravity if holding down
				SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
				//Caps maximum fall speed
				RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFastFallSpeed));
			}
			else if (_isJumpCut)
			{
				//Higher gravity if jump button released
				SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
				RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
			}
			else if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
			{
				SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
			}
			else if (RB.velocity.y < 0)
			{
				//Higher gravity if falling
				SetGravityScale(Data.gravityScale * Data.fallGravityMult);
				//Caps maximum fall speed
				RB.velocity = new Vector2(RB.velocity.x, Mathf.Max(RB.velocity.y, -Data.maxFallSpeed));
			}
			else
			{
				//Default gravity if standing on a platform or moving upwards
				SetGravityScale(Data.gravityScale);
			}
		}
		else
		{
			//No gravity when dashing
			SetGravityScale(0);
		}
		#endregion

		UpdateAnimation();
	}

	private void FixedUpdate()
	{
		//Handle Run
		if (!IsDashing)
		{
			if (IsWallJumping)
				Run(Data.wallJumpRunLerp);
			else
				Run(1);
			//running anim
		}
		else if (_isDashAttacking)
		{
			Run(Data.dashEndRunLerp);
		}

		//Handle Slide
		if (IsSliding)
			Slide();
		//roll anim
	}

	private void UpdateAnimation()
    {
		//Run anim
		if (_moveInput.y == 0f)
		{
            if (_moveInput.x != 0f)
            {
				anim.SetBool("running", true);
				anim.SetBool("idling", false);
				anim.SetBool("rolling", false);
				anim.SetBool("jumping", false);
			}
			else if(_moveInput.x == 0f)
            {
				anim.SetBool("idling", true);
				anim.SetBool("running", false);
				anim.SetBool("rolling", false);
				anim.SetBool("jumping", false);
			}
			else if (IsDashing)
            {
				anim.SetBool("rolling", true);
				anim.SetBool("running", false);
				anim.SetBool("idling", false);
				anim.SetBool("jumping", false);
			}
			else if (IsJumping)
            {
				anim.SetBool("jumping", true);
				anim.SetBool("running", false);
				anim.SetBool("rolling", false);
				anim.SetBool("idling", false);
			}
			
		}
	
	}



    #region INPUT CALLBACKS
    //Methods which whandle input detected in Update()
    public void OnJumpInput()
	{
		LastPressedJumpTime = Data.jumpInputBufferTime;
	}

	public void OnJumpUpInput()
	{
		if (CanJumpCut() || CanWallJumpCut())
			_isJumpCut = true;
	}

	public void OnDashInput()
	{
		LastPressedDashTime = Data.dashInputBufferTime;
	}
	#endregion

	#region HELPER METHODS
	public void SetGravityScale(float scale)
	{
		RB.gravityScale = scale;
	}

	private void Sleep(float duration)
	{
		StartCoroutine(nameof(PerformSleep), duration);
	}

	private IEnumerator PerformSleep(float duration)
	{
		Time.timeScale = 0;
		yield return new WaitForSecondsRealtime(duration);
		Time.timeScale = 1;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
		Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
	}
	#endregion

	#region MOVEMENT METHODS

	#region RUN METHODS
	private void Run(float lerpAmount)
	{
		float targetSpeed = _moveInput.x * Data.runMaxSpeed;
		targetSpeed = Mathf.Lerp(RB.velocity.x, targetSpeed, lerpAmount);

		#region Calculate AccelRate
		float accelRate;

		if (LastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;
		#endregion

		#region Add Bonus Jump Apex Acceleration
		if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold)
		{
			accelRate *= Data.jumpHangAccelerationMult;
			targetSpeed *= Data.jumpHangMaxSpeedMult;
		}
		#endregion

		#region Conserve Momentum

		if (Data.doConserveMomentum && Mathf.Abs(RB.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(RB.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
		{
			accelRate = 0;
		}
		#endregion

		float speedDif = targetSpeed - RB.velocity.x;

		float movement = speedDif * accelRate;

		RB.AddForce(movement * Vector2.right, ForceMode2D.Force);

	}

	private void Turn()
	{
		//stores scale and flips the player along the x axis, 
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;

		IsFacingRight = !IsFacingRight;
	}
	#endregion

	#region JUMP METHODS
	private void Jump()
	{

		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;
		_isGrounded = false;
		float force = Data.jumpForce;
		if (RB.velocity.y < 0)
			force -= RB.velocity.y;

		RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
		

	}

	private void WallJump(int dir)
	{

		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;
		LastOnWallRightTime = 0;
		LastOnWallLeftTime = 0;

		#region Perform Wall Jump
		Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
		force.x *= dir;

		if (Mathf.Sign(RB.velocity.x) != Mathf.Sign(force.x))
			force.x -= RB.velocity.x;

		if (RB.velocity.y < 0)
			force.y -= RB.velocity.y;


		RB.AddForce(force, ForceMode2D.Impulse);
		#endregion
	}
	#endregion

	#region DASH METHODS
	private IEnumerator StartDash(Vector2 dir)
	{

		LastOnGroundTime = 0;
		LastPressedDashTime = 0;

		float startTime = Time.time;

		_dashesLeft--;
		_isDashAttacking = true;

		SetGravityScale(0);

		while (Time.time - startTime <= Data.dashAttackTime)
		{
			RB.velocity = dir.normalized * Data.dashSpeed;
			yield return null;
		}

		startTime = Time.time;

		_isDashAttacking = false;

		SetGravityScale(Data.gravityScale);
		RB.velocity = Data.dashEndSpeed * dir.normalized;

		while (Time.time - startTime <= Data.dashEndTime)
		{
			yield return null;
		}

		IsDashing = false;
	}

	private IEnumerator RefillDash(int amount)
	{
		_dashRefilling = true;
		yield return new WaitForSeconds(Data.dashRefillTime);
		_dashRefilling = false;
		_dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
	}

	private void Slide()
	{
		if (RB.velocity.y > 0)
		{
			RB.AddForce(-RB.velocity.y * Vector2.up, ForceMode2D.Impulse);
		}
		float speedDif = Data.slideSpeed - RB.velocity.y;
		float movement = speedDif * Data.slideAccel;

		movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

		RB.AddForce(movement * Vector2.up);
	}
	#endregion

	#endregion

	#region CHECK METHODS
	public void CheckDirectionToFace(bool isMovingRight)
	{
		if (isMovingRight != IsFacingRight)
			Turn();
	}

	private bool CanJump()
	{
		return (LastOnGroundTime > 0) && !IsJumping && _isGrounded;
	}

	private bool CanWallJump()
	{
		return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (!IsWallJumping ||
			 (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
	}

	private bool CanJumpCut()
	{
		return IsJumping && RB.velocity.y > 0;
	}

	private bool CanWallJumpCut()
	{
		return IsWallJumping && RB.velocity.y > 0;
	}

	private bool CanDash()
	{
		if (!IsDashing && _dashesLeft < Data.dashAmount && LastOnGroundTime > 0 && !_dashRefilling)
		{
			StartCoroutine(nameof(RefillDash), 1);
		}

		return _dashesLeft > 0;
	}

	public bool CanSlide()
	{
		if (LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0)
			return true;
		else
			return false;
	}
	#endregion


}


