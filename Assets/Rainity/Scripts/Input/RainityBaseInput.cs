using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class RainityBaseInput : BaseInput
{
	private string _compositionString = string.Empty;
	private IMECompositionMode _imeCompositionMode = IMECompositionMode.Auto;
	private Vector2 _compositionCursorPos;

	protected override void OnEnable()
	{
		base.OnEnable();
		if (Keyboard.current != null)
			Keyboard.current.onIMECompositionChange += OnIMECompositionChange;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (Keyboard.current != null)
			Keyboard.current.onIMECompositionChange -= OnIMECompositionChange;
	}

	private void OnIMECompositionChange(IMECompositionString composition)
	{
		_compositionString = composition.ToString();
	}

	public override string compositionString
	{
		get { return _compositionString; }
	}

	public override IMECompositionMode imeCompositionMode
	{
		get { return _imeCompositionMode; }
		set
		{
			_imeCompositionMode = value;
			Keyboard.current?.SetIMEEnabled(value != IMECompositionMode.Off);
		}
	}

	public override Vector2 compositionCursorPos
	{
		get { return _compositionCursorPos; }
		set
		{
			_compositionCursorPos = value;
			Keyboard.current?.SetIMECursorPosition(value);
		}
	}

	public override bool mousePresent
	{
		get { return true; }
	}

	public override bool GetMouseButtonDown(int button)
	{
		return RainityInput.GetMouseButtonDown(button);
	}

	public override bool GetMouseButtonUp(int button)
	{
		return RainityInput.GetMouseButtonUp(button);
	}

	public override bool GetMouseButton(int button)
	{
		return RainityInput.GetMouseButton(button);
	}

	public override Vector2 mousePosition
	{
		get { return RainityInput.mousePosition; }
	}

	public override Vector2 mouseScrollDelta
	{
		get { return Mouse.current?.scroll.ReadValue() ?? Vector2.zero; }
	}

	public override bool touchSupported
	{
		get { return Touchscreen.current != null; }
	}

	public override int touchCount
	{
		get { return 0; }
	}

	public override Touch GetTouch(int index)
	{
		return default(Touch);
	}

	public override float GetAxisRaw(string axisName)
	{
		return 0f;
	}

	public override bool GetButtonDown(string buttonName)
	{
		return RainityInput.GetButtonDown(buttonName);
	}
}