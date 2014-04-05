using UnityEngine;
using System.Collections;

public class GunData {
	public string Name = "default name";
	public Texture Pic;
	public Material Mat;
	public GameObject Prefab;
	public bool AutoFire = false;
	public float Delay = 0f;
	public float Cooldown = 0f; // current progress through the ^above^ Delay
	public bool Carrying = false;	
}