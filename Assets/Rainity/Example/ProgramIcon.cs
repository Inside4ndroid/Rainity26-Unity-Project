using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class ProgramIcon : MonoBehaviour {

	public Renderer iconVisual;
	public string filePath;

	Transform iconParent;
	bool mouseOver = false;

	Camera _cam;
	Vector3 _iconVelocity;
	const float _smoothTime = 0.1f;

	// Use this for initialization
	void Start () {
		filePath = filePath.Replace("\"", "");
		if (File.Exists(filePath)) {
			iconVisual.material.mainTexture = Rainity.GetFileIcon(filePath);
			//border.material.SetColor("_Color", Rainity.GetAverageColorOfTexture((Texture2D)iconVisual.material.mainTexture));
		} else {
			UnityEngine.Debug.Log("Please assign a valid file path to the ProgramIcon script on " + transform.name + ".");
		}

		iconParent = transform.GetChild(0);
		_cam = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
		// OnMouseEnter/Exit/Over only fire when the Unity window receives OS mouse messages,
		// which it never does when parented into the WorkerW desktop layer.
		// Instead, raycast each frame using the position from the low-level mouse hook.
		if (_cam != null) {
			Ray ray = _cam.ScreenPointToRay(RainityInput.mousePosition);
			RaycastHit hit;
			mouseOver = Physics.Raycast(ray, out hit) && hit.transform == transform;
			if (mouseOver && RainityInput.GetMouseButtonDown(0)) {
				Rainity.OpenFile(filePath);
			}
		}

		// SmoothDamp gives a frame-rate-independent, ease-in/ease-out spring motion.
		Vector3 target = new Vector3(transform.position.x, mouseOver ? -0.5f : 0f, transform.position.z);
		iconParent.position = Vector3.SmoothDamp(iconParent.position, target, ref _iconVelocity, _smoothTime);
	}
}
