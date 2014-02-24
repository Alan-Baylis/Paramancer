using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityClass : MonoBehaviour {
	public NetUser User;
	public float EnergyLeft = 1f; // 0-1
	private CharacterController cc;
	private Avatar ava;

	// cam
	public GameObject camHolder;
	private Vector3 camAngle;
	
	private bool crouched = false;
	public GameObject weaponSoundObj;
	
	public string offeredPickup = "";
	public PickupBoxScript currentOfferedPickup;
	
	public GameObject animObj;
	
	// swapper
	public int swapperCrossX = 0;
	public int swapperCrossY = 0;
	public bool swapperLocked = false;
	private Vector3 swapperLock = Vector3.zero;
	
	private Vector3 moveVec = Vector3.zero;
	
	public GameObject gravArrowPrefab;
	private GameObject gravArrowObj;
	
	public GameObject bballArrowPrefab;
	private GameObject bballArrowObj;
	
	public Material invisibleMat;
	public Material gunMat;
	public Material dummyAMat;
	public Material dummyBMat;
	public Material dummyCMat;
	public Material boxMat;
	public Material fishMat;
	public Material bananaMat;
	public Material creeperMat;
	public Material elephantMat;
	public Material moonMat;
	public Material pyramidMat;
	public Material chocoboMat;
	public Material spikeMat;
	public Material tentacleMat;
	public Material robotHeadMat;
	public Material speaceshipMat;
	public Material enforcerMat;
	public Material smileyMat;
	public Material helmetMat;
	public Material paperbagMat;
	public Material maheadMat;
	
	public Color colA;
	public Color colB;
	public Color colC;
	public int headType = 0;
	
	public GameObject meshObj;
	public GameObject gunMesh1;
	public GameObject gunMesh2;
	
	public GameObject[] heads;
	
	public bool grounded;
	public float yMove = 0f;

	private double lastUpdateTime = -1f;
	
	private Vector3 lastCamAngle = Vector3.zero;
	private Vector3 lastMoveVector = Vector3.zero;
	private bool prevCrouching = false;
	private float lastYmove = 0f;
	private float lastHealth = 0f;
	
	public Light firstPersonLight;
	
	// gun related
	public Item GunInHand = Item.Pistol;
	public float GunInHandCooldown = 0f;
	public Item GunOnBack = Item.Grenade;
	private float gunOnBackCooldown = 0f;
	private Item prevGunInHand = Item.None;
	private Item prevGunOnBack = Item.None;
	public GameObject firstPersonGun;
	int swapperLockTarget = -1;
	
	// network
	public bool isLocal = true;
	public bool Spectating = false;
	public int Spectatee = 0;
	
	// scripts
	public CcNet net;
	Hud hud; // won't need this anymore once playingHud gets drawn correctly? *****************
	Arsenal arse;
	LocalUser locUser;
	
	
	
	void Start() {
		var o = GameObject.Find("Main Program");
		net = o.GetComponent<CcNet>();
		hud = o.GetComponent<Hud>();
		arse = o.GetComponent<Arsenal>();
		locUser = o.GetComponent<LocalUser>();
		
		if (User.local && net.CurrMatch.pitchBlack) {
			Camera.main.backgroundColor = Color.black;
			RenderSettings.ambientLight = Color.black;
		}
		
		cc = GetComponent<CharacterController>();
		
		if (ava == null) 
			ava = gameObject.AddComponent<Avatar>();
		
		if (User.lives >= 0) {
			if (isLocal) {
				SetModelVisibility(false);
			}else{
				SetModelVisibility(true);
			}
			
			Respawn();
		}else{
			//we joined as a spectator
			SetModelVisibility(false);
			transform.position = -Vector3.up * 99f;
		}
	}
	
	public GameObject ourKiller;
	public bool sendRPCUpdate = false;
	float rpcCamtime = 0f;
	void Update() {
		if (User.local)
			net.localPlayer.Entity = this;
		
		if (User.health <= 0f) {
			// shut off bomb
			if (gunMesh1 != null && gunMesh1.transform.Find("Flash Light") != null) {
				gunMesh1.transform.Find("Flash Light").GetComponent<FlashlightScript>().visible = false;
			}
			if (gunMesh2 != null && gunMesh2.transform.Find("Flash Light") != null) {
				gunMesh2.transform.Find("Flash Light").GetComponent<FlashlightScript>().visible = false;
			} 
		}
		
		AudioListener.volume = net.gameVolume;
		
		if (Spectating && isLocal) {
			if (net.players.Count > 0) {
				if (firstPersonGun) 
					firstPersonGun.renderer.enabled = false;
				
				if (InputUser.Started(UserAction.Activate) ||
					net.players[Spectatee].lives <= 0
				) {
					Spectatee++;
					
					if (Spectatee >= net.players.Count) 
						Spectatee = 0;
				}
				
				Camera.main.transform.parent = null;
				Camera.main.transform.position = net.players[Spectatee].Entity.transform.position;
				float invY = 1f;
				if (locUser.LookInvert)
					invY = -1f;
				camAngle.x -= Input.GetAxis("Mouse Y") * Time.deltaTime * 30f * locUser.mouseSensitivity * invY;
				camAngle.y += Input.GetAxis("Mouse X") * Time.deltaTime * 30f * locUser.mouseSensitivity;
				if (camAngle.x>85f) camAngle.x = 85f;
				if (camAngle.x<-85f) camAngle.x = -85f;
				Camera.main.transform.eulerAngles = camAngle;
				Camera.main.transform.Translate(0,0,-3);
			}
			
			return;
		}
		
		if (isLocal) {
			if (!Spectating) {
				Vector3 lastPos = transform.position;
				if (User.health > 0f) {
					if (offeredPickup != "") {
						bool pickup = false;

						if (!net.autoPickup && InputUser.Started(UserAction.GrabItem)) 
							pickup = true;
						if (net.autoPickup) 
							pickup = true;
						
						if (pickup) {
							for (int i=0; i<arse.Guns.Length; i++) {
								if (offeredPickup == arse.Guns[i].Name) {
									GunInHand = (Item)i;
									GunInHandCooldown = 0f;
									gunRecoil += Vector3.right * 5f;
									gunRecoil -= Vector3.up * 5f;
									PlaySound("weaponChange");
									currentOfferedPickup.Pickup();
								}
							}
							
							if (offeredPickup == "Health" && User.health < 100f){
								net.ConsumeHealth(User.viewID);
								net.localPlayer.health = 100f;
								User.health = 100f;
								PlaySound("weaponChange");
								currentOfferedPickup.Pickup();
							}
						}
					}
				}
				
				offeredPickup = "";
				
				if (User.health > 0f) {
					if (Camera.main.transform.parent == null) 
						SetModelVisibility(false);
					
					ourKiller = null;
					Camera.main.transform.parent = camHolder.transform;
					Camera.main.transform.localPosition = Vector3.zero;
					//Camera.main.transform.localEulerAngles = Vector3.zero;
					Camera.main.transform.localRotation = Quaternion.Slerp(Camera.main.transform.localRotation, Quaternion.Euler(new Vector3(0,0,0)), Time.deltaTime * 5f);
					
					float invY = 1f;
					if (locUser.LookInvert) 
						invY = -1f;
					
					if (hud.Mode == HudMode.Playing || 
						hud.Mode == HudMode.Editing
					) {
						camAngle.x -= Input.GetAxis("Mouse Y") * Time.deltaTime * 30f * locUser.mouseSensitivity * invY;
						camAngle.y += Input.GetAxis("Mouse X") * Time.deltaTime * 30f * locUser.mouseSensitivity;
						if (camAngle.x >  85f) 
							camAngle.x =  85f;
						if (camAngle.x < -85f) 
							camAngle.x = -85f;
					}
					
					camHolder.transform.localEulerAngles = camAngle;
					bool startedSprinting = false;
					Vector3 inputVector = Vector3.zero; 

					if (InputUser.Holding(UserAction.MoveForward)) 
						inputVector += animObj.transform.forward;
					
					if (InputUser.Holding(UserAction.MoveBackward)) 
						inputVector -= animObj.transform.forward;
					
					if (InputUser.Holding(UserAction.MoveRight)) 
						inputVector += animObj.transform.right;
					
					if (InputUser.Holding(UserAction.MoveLeft)) 
						inputVector -= animObj.transform.right;
					
					if (InputUser.Started(UserAction.Sprint)) 
						startedSprinting = true;
					
					//inputVector.y = 0f;
					inputVector.Normalize();

					ava.UpVector = animObj.transform.up;
					
					if (!crouched) {
						ava.Move(inputVector * Time.deltaTime * 10f, startedSprinting);
					}else{
						ava.Move(inputVector * Time.deltaTime * 5f);
					}
					
					EnergyLeft = ava.GetEnergy();
					
					
					if (yMove <= 0f) {
						ava.Move(transform.up * -0.2f);
						bool landed = grounded;
						grounded = ava.isGrounded;
						
						if (!grounded) 
							ava.Move(transform.up * 0.2f);
						
						if (!landed && grounded) {
							PlaySound("land");
							sendRPCUpdate = true;
						}
					}else{
						grounded = false;
					}
					
					if (grounded) {
						yMove = 0f;
						if (InputUser.Started(UserAction.MoveUp)) {
							yMove = 4f;
							PlaySound("jump");
							net.SendTINYUserUpdate(User.viewID, UserAction.MoveUp);
						}
					}else{
						yMove -= Time.deltaTime * 10f;
					}
					
					ava.Move(transform.up * yMove * Time.deltaTime * 5f);
					
					crouched = false;
					if (InputUser.Holding(UserAction.MoveDown)) 
						crouched = true;
					
					moveVec = inputVector;
					
					Ray lavaRay = new Ray(lastPos, transform.position - lastPos);
					RaycastHit lavaHit = new RaycastHit();
					float lavaRayLength = Vector3.Distance(transform.position, lastPos);
					int lavaLayer = (1<<10);
					if (Physics.Raycast(lavaRay, out lavaHit, lavaRayLength, lavaLayer)) {
						transform.position = lavaHit.point+ (Vector3.up*0.35f);
						sendRPCUpdate = true;
						inputVector = Vector3.zero;
						net.RegisterHit(Item.Lava, User.viewID, User.viewID, lavaHit.point);
					}
					
					
					//sendRPCUpdate = false;
					if (camAngle != lastCamAngle && Time.time > rpcCamtime) 
						sendRPCUpdate = true;
					if (moveVec != lastMoveVector) 
						sendRPCUpdate = true;
					if (crouched != prevCrouching) 
						sendRPCUpdate = true;
					//if (yMove != lastYmove) sendRPCUpdate = true;
					if (User.health != lastHealth) 
						sendRPCUpdate = true;
					if (net.broadcastPos) {
						net.broadcastPos = false;
						sendRPCUpdate = true;
					}
					
					lastCamAngle = camAngle;
					lastMoveVector = moveVec;
					prevCrouching = crouched;
					lastYmove = yMove;
					lastHealth = User.health;
					
					if (sendRPCUpdate) {
						net.SendUserUpdate(User.viewID, transform.position, camAngle, crouched, moveVec, yMove, 
							(int)GunInHand, (int)GunOnBack, transform.up, transform.forward);
						sendRPCUpdate = false;
						
						rpcCamtime = Time.time; // + 0.02f;
					}
					
					if (GunInHand >= 0 && GunInHandCooldown > 0f && 
						GunInHandCooldown - Time.deltaTime <= 0f && 
						arse.Guns[(int)GunInHand].Delay >= 1f
					) 
						PlaySound("reload");
					
					GunInHandCooldown -= Time.deltaTime;
					if (GunInHandCooldown < 0f) 
						GunInHandCooldown = 0f;
					
					
					swapperLocked = false;
					swapperLockTarget = -1;
					if (GunInHand == Item.Swapper) {
						// swapper aiming
						List<int> validSwapTargets = new List<int>();
						
						for (int i=0; i<net.players.Count; i++){
							if (!net.players[i].local && Vector3.Dot(Camera.main.transform.forward, (net.players[i].Entity.transform.position - Camera.main.transform.position).normalized) > 0.94f && net.players[i].health>0f){
								
								Ray swapCheckRay = new Ray(Camera.main.transform.position, net.players[i].Entity.transform.position - Camera.main.transform.position);
								RaycastHit swapCheckHit = new RaycastHit();
								int swapCheckLayer = 1<<0;
								float swapCheckLength = Vector3.Distance(net.players[i].Entity.transform.position, Camera.main.transform.position);
								
								if (!Physics.Raycast(swapCheckRay, out swapCheckHit, swapCheckLength, swapCheckLayer) ) {
									validSwapTargets.Add(i);
									swapperLocked = true;
								}
							}
						}
						int nearestScreenspacePlayer = 0;
						float nearestDistance = 9999f;
						for (int i=0; i<validSwapTargets.Count; i++) {
							Vector3 thisPos = Camera.main.WorldToScreenPoint(net.players[validSwapTargets[i]].Entity.transform.position);
							if (Vector3.Distance(thisPos, 
								new Vector3(Screen.width/2, Screen.height/2, 0)) < nearestDistance
							) {
								nearestScreenspacePlayer = validSwapTargets[i];
							}
						}
						
						if (swapperLocked) {
							// move target to locked on player
							Vector3 screenPos = Camera.main.WorldToScreenPoint(net.players[nearestScreenspacePlayer].Entity.transform.position);
							swapperLock -= (swapperLock-screenPos) * Time.deltaTime * 10f;
							swapperLockTarget = nearestScreenspacePlayer;
						}else{
							// move target to center
							swapperLock -= (swapperLock-new Vector3(Screen.width/2, Screen.height/2, 0)) * Time.deltaTime * 10f;
						}
					}else{
						swapperLock = new Vector3(Screen.width/2, Screen.height/2, 0);
					}
					
					swapperCrossX = Mathf.RoundToInt(swapperLock.x);
					swapperCrossY = Mathf.RoundToInt(swapperLock.y);
					
					// basketball arrow
					if (net.CurrMatch.basketball) {
						if (bballArrowObj == null) {
							bballArrowObj = (GameObject)GameObject.Instantiate(bballArrowPrefab);
							bballArrowObj.transform.parent = Camera.main.transform;
							bballArrowObj.transform.localPosition = Vector3.forward - (Vector3.right*0.8f) + (Vector3.up*0.5f);
						}
						if (User.hasBall) {
							bballArrowObj.renderer.enabled = false;
						}else{
							bballArrowObj.renderer.enabled = true;
							bballArrowObj.transform.LookAt(net.GetBball().transform.position);
							
						}
					}else{
						if (bballArrowObj != null) {
							bballArrowObj.renderer.enabled = false;
						}
					}
					
					// grav gun arrow
					if (GunInHand == Item.GravGun) {
						if (gravArrowObj == null) {
							gravArrowObj = (GameObject)GameObject.Instantiate(gravArrowPrefab);
							//gravArrowObj.layer = 
							gravArrowObj.transform.parent = Camera.main.transform;
							gravArrowObj.transform.localPosition = Vector3.forward;
						}
						
						Ray gravRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
						RaycastHit gravHit = new RaycastHit();
						int gravLayer = 1<<0;
						
						if (Physics.Raycast(gravRay, out gravHit, 999f, gravLayer)) {
							gravArrowObj.transform.LookAt(gravArrowObj.transform.position - gravHit.normal);
							gravArrowObj.renderer.enabled = true;
						}else{
							gravArrowObj.renderer.enabled = false;
						}
					}else{
						if (gravArrowObj != null){
							gravArrowObj.renderer.enabled = false;
						}
					}
					
					if /* shooting */ 
						(Screen.lockCursor && 
						!User.hasBall && 
						GunInHand >= Item.Pistol
					) {
						if (arse.Guns[(int)GunInHand].AutoFire) {
							if /* holding */ 
								(InputUser.Holding(UserAction.Activate)
							) {
								if (GunInHandCooldown <= 0f) {
									Fire();
									GunInHandCooldown += arse.Guns[(int)GunInHand].Delay;
								}
							}
						}else{
							if /* started pressing */ 
								(InputUser.Started(UserAction.Activate)
							) {
								if (GunInHandCooldown <= 0f) {
									Fire();
									GunInHandCooldown += arse.Guns[(int)GunInHand].Delay;
								}
							}
						}
					}
					
					if (InputUser.Started(UserAction.SwapWeapon) ) {
						// swap guns
						Item gun = GunInHand;
						float tempFloat = GunInHandCooldown;
						GunInHand = GunOnBack;
						GunInHandCooldown = gunOnBackCooldown;
						GunOnBack = gun;
						gunOnBackCooldown = tempFloat;
						
						gunRecoil += Vector3.right * 5f;
						gunRecoil -= Vector3.up * 5f;
						PlaySound("weaponChange");
						net.SendTINYUserUpdate(User.viewID, UserAction.SwapWeapon);
					}
					
					// ball throwing
					if (InputUser.Started(UserAction.Activate) &&
						Screen.lockCursor && 
						User.hasBall
					) {
						net.ThrowBall(Camera.main.transform.position, Camera.main.transform.forward, 20f);
					}
					
					if (InputUser.Started(UserAction.Suicide)) {
						net.RegisterHitRPC((int)Item.Suicide, User.viewID, User.viewID, transform.position);
					}
					
					moveFPGun();
				}else{ // we be dead
					if (Camera.main.transform.parent != null) 
						SetModelVisibility(true);
					
					if (ourKiller != null) {
						Camera.main.transform.parent = null;
						Camera.main.transform.position = transform.position - animObj.transform.forward;
						Camera.main.transform.LookAt(ourKiller.transform.position,transform.up);
						Camera.main.transform.Translate(0, 0, -2f);
					}
				}
			}
		}else{
			if (lastUpdateTime > 0f) {
				NonLocalUpdate();
			}
		}
		
		if (!crouched) {
			camHolder.transform.localPosition = Vector3.up * 0.7f;
		}else{
			camHolder.transform.localPosition = Vector3.zero;
		}
		
		// visible person model anims
		if (!User.local) 
			camHolder.transform.localEulerAngles = camAngle;
		
		Vector3 lookDir = camHolder.transform.forward;
		//lookDir.y = 0;
		lookDir.Normalize();
		animObj.transform.LookAt(animObj.transform.position + lookDir,transform.up);
		animObj.transform.localEulerAngles = new Vector3(0, animObj.transform.localEulerAngles.y, 0);
		
		showCorrectGuns();

//		// animations
//		if (User.health > 0f) {
//			if (yMove == 0f) {
//				if (moveVec.magnitude > 0.1f) {
//					if (crouched) {
//						animObj.animation.Play("crouchrun");
//					}else{
//						animObj.animation.Play("run");
//					}
//					
//					if (Vector3.Dot(moveVec, lookDir) < -0.5f) {
//						animObj.animation["crouchrun"].speed = -1;
//						animObj.animation["run"].speed = -1;
//					}else{
//						animObj.animation["crouchrun"].speed = 1;
//						animObj.animation["run"].speed = 1;
//					}
//				}else{
//					if (crouched) {
//						animObj.animation.Play("crouch");
//					}else{
//						animObj.animation.Play("idle");
//					}
//				}
//			}else{
//				if (yMove > 0f) {
//					animObj.animation.Play("rise");
//				}else{
//					animObj.animation.Play("fall");
//				}
//			}
//		}else{
//			animObj.animation.Play("die");
//		}
		
		// if dead, make unshootable
		if (User.health > 0f) {
			gameObject.layer = 8;
		}else{
			gameObject.layer = 2;
		}
		
		// if no friendly fire & on same team, make unshootable
		if (net.CurrMatch.teamBased && !net.CurrMatch.allowFriendlyFire) {
			if (User.team == net.localPlayer.team) {
				gameObject.layer = 2;
			}
		}
		
		if (User.hasBall) {
			if (User.local && firstPersonGun && firstPersonGun.renderer) 
				firstPersonGun.renderer.enabled = false;
		}else{
			if (User.local && firstPersonGun && firstPersonGun.renderer && User.health > 0f) 
				firstPersonGun.renderer.enabled = true;
		}
	}

	void showCorrectGuns() {
		if (GunInHand != prevGunInHand) {
			Transform gunParent = gunMesh1.transform.parent;
			Destroy(gunMesh1);

			if (GunInHand >= Item.Pistol) {
				gunMesh1 = (GameObject)GameObject.Instantiate(arse.Guns[(int)GunInHand].Prefab);
			}else{
				gunMesh1 = new GameObject();
			}
			
			gunMesh1.transform.parent = gunParent;
			gunMesh1.transform.localEulerAngles = new Vector3(0, 180, 90);
			gunMesh1.transform.localPosition = Vector3.zero;
			prevGunInHand = GunInHand;
			
			if (User.local) {
				if (firstPersonGun != null) 
					Destroy(firstPersonGun);
				
				if (GunInHand >= Item.Pistol) {
					firstPersonGun = (GameObject)GameObject.Instantiate(arse.Guns[(int)GunInHand].Prefab);
				}else{
					firstPersonGun = new GameObject();
				}
				
				firstPersonGun.transform.parent = Camera.main.transform;
				firstPersonGun.transform.localEulerAngles = new Vector3( -90, 0, 0);
				firstPersonGun.transform.localPosition = new Vector3( 0.47f, -0.48f, 0.84f);
				
				if (firstPersonGun.renderer) 
					firstPersonGun.renderer.castShadows = false;
			}
			
			sendRPCUpdate = true;
			
			if (User.health <= 0f || !User.local) {
				SetModelVisibility(true);
			}else{
				SetModelVisibility(false);
			}
		}

		if (GunOnBack != prevGunOnBack) {
			Transform gunParentB = gunMesh2.transform.parent;
			Destroy(gunMesh2);
			
			if (GunOnBack >= Item.Pistol) {
				gunMesh2 = (GameObject)GameObject.Instantiate(arse.Guns[(int)GunOnBack].Prefab);
			}else{
				gunMesh2 = new GameObject();
			}
			
			gunMesh2.transform.parent = gunParentB;
			gunMesh2.transform.localEulerAngles = new Vector3(0,180,90);
			gunMesh2.transform.localPosition = Vector3.zero;
			prevGunOnBack = GunOnBack;
			sendRPCUpdate = true;
			
			if (User.health <= 0f || !User.local) {
				SetModelVisibility(true);
			}else{
				SetModelVisibility(false);
			}
		}
	}
	
	public GameObject aimBone;
	private Vector3 gunInertia = Vector3.zero;
	private Vector3 gunRecoil = Vector3.zero;
	private Vector3 gunRot = Vector3.zero;
	private float gunBounce = 0f;
	void moveFPGun() {
		if (firstPersonGun == null) 
			return;
		
		// angle
		firstPersonGun.transform.eulerAngles = gunRot;
		Quaternion fromRot = firstPersonGun.transform.rotation;
		firstPersonGun.transform.localEulerAngles = new Vector3( -90, 0, 0);
		firstPersonGun.transform.rotation = Quaternion.Slerp(fromRot, firstPersonGun.transform.rotation, Time.deltaTime * 30f);
		gunRot = firstPersonGun.transform.eulerAngles;
		
		firstPersonGun.transform.localPosition = new Vector3( 0.47f, -0.48f, 0.84f);
		
		gunInertia -= (gunInertia-new Vector3(0f, yMove, 0f)) * Time.deltaTime * 5f;
		if (gunInertia.y < -3f) 
			gunInertia.y = -3f;
		firstPersonGun.transform.localPosition += gunInertia * 0.1f;
		
		float recoilRest = 5f;
		switch ((Item)GunInHand) {
			case Item.Pistol:
				recoilRest = 5f; break;
			case Item.Grenade:
				recoilRest = 8f; break;
			case Item.MachineGun:
				recoilRest = 8f; break;
			case Item.Rifle:
				recoilRest = 2f; break;
			case Item.RocketLauncher:
				recoilRest = 1f; break;
			case Item.Spatula:
				recoilRest = 2f; break;
		}
		
		gunRecoil -= (gunRecoil-Vector3.zero) * Time.deltaTime * recoilRest;
		firstPersonGun.transform.localPosition += gunRecoil * 0.1f;
		
		if (grounded) {
			if (moveVec.magnitude > 0.1f && net.gunBobbing){
				if (crouched){
					gunBounce += Time.deltaTime * 6f;
				}else{
					gunBounce += Time.deltaTime * 15f;
				}
				firstPersonGun.transform.position += Vector3.up * Mathf.Sin(gunBounce) *0.05f;
			}
			
		}
	}
	
	void Fire() {
		Item it = (Item)GunInHand;
		switch (it) {
			case Item.Pistol:
				FireBullet(it);
				gunRecoil -= Vector3.forward * 2f;
				break; 
			case Item.Grenade:
			net.Shoot(it, Camera.main.transform.position, Camera.main.transform.forward, Camera.main.transform.position + Camera.main.transform.forward, net.localPlayer.viewID, false, ava.sprinting);
				gunRecoil += Vector3.forward * 6f;
				break; 
			case Item.MachineGun:
				FireBullet(it);
				gunRecoil -= Vector3.forward * 2f;
				gunRecoil += new Vector3(Random.Range(-1f,1f),Random.Range(-1f,1f),Random.Range(-1f,1f)).normalized * 0.2f;
				break; 
			case Item.Rifle:
				FireBullet(it);
				gunRecoil -= Vector3.forward * 5f;
				break; 
			case Item.RocketLauncher:
				net.Shoot(it, Camera.main.transform.position, Camera.main.transform.forward, Camera.main.transform.position + Camera.main.transform.forward, net.localPlayer.viewID, false);
				gunRecoil -= Vector3.forward * 5f;
				break; 
			case Item.Swapper:
				if (swapperLockTarget == -1) {
					// not locked on, we miss
					FireBullet(it);
				}else{
					// locked on, we hit
					net.Shoot(it, transform.position, net.players[swapperLockTarget].Entity.transform.position - transform.position, net.players[swapperLockTarget].Entity.transform.position , net.localPlayer.viewID, true);
					net.RegisterHit(it, net.localPlayer.viewID, net.players[swapperLockTarget].viewID, net.players[swapperLockTarget].Entity.transform.position);
				}
				gunRecoil -= Vector3.forward * 5f;
				break; 
			case Item.GravGun:
				Ray gravRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
				RaycastHit gravHit = new RaycastHit();
				int gravLayer = 1<<0;
			
				if (Physics.Raycast(gravRay, out gravHit, 999f, gravLayer)) {
					Vector3 lookPos = Camera.main.transform.position + Camera.main.transform.forward;
					Quaternion tempRot = Camera.main.transform.rotation;
					transform.LookAt(transform.position + Vector3.Cross(Camera.main.transform.forward,gravHit.normal), gravHit.normal);
					ForceLook(lookPos);
					camHolder.transform.localEulerAngles = camAngle;
					Camera.main.transform.rotation = tempRot;
					PlaySound("gravgun");
				}
				
				sendRPCUpdate = true;
				gunRecoil -= Vector3.forward * 5f;
				break; 
			case Item.Bomb:
				net.Detonate(it, transform.position, User.viewID, User.viewID);
				break; 
			case Item.Spatula:
				// FIXME: IF WE KEEP THIS, IT SHOULD BE AN INSTAGIB MELEE WEAPON
				
				//gunRecoil += Vector3.forward * 6f;
				gunRecoil -= Vector3.right * 4f;
				break;
		}
	}
	
	public void SetModelVisibility(bool visible) {
		Material[] mats = meshObj.renderer.materials;
		Material newMatA = new Material(dummyAMat);
		Material newMatB = new Material(dummyBMat);
		Material newMatC = new Material(dummyCMat);
		newMatA.color = User.colA;
		newMatB.color = User.colB;
		newMatC.color = User.colC;
		
		if (net.CurrMatch.teamBased) {
			if (User.team == 1) {
				newMatA.color = Color.red;
			}
			if (User.team == 2) {
				newMatA.color = Color.cyan;
			}
		}
		
		if (!visible) {
			mats[0] = invisibleMat;
			mats[1] = invisibleMat;
			mats[2] = invisibleMat;
			meshObj.renderer.materials = mats;
			
			if (gunMesh1.renderer) 
				gunMesh1.renderer.material = invisibleMat;
			if (gunMesh2.renderer) 
				gunMesh2.renderer.material = invisibleMat;
			
			if (GunInHand == Item.Bomb) {
				if (gunMesh1!= null && gunMesh1.transform.Find("Flash Light") != null) {
					gunMesh1.transform.Find("Flash Light").GetComponent<FlashlightScript>().visible = false;
				}
			}
		}else{
			mats[0] = newMatA;
			mats[1] = newMatB;
			mats[2] = newMatC;
			meshObj.renderer.materials = mats;
			
			if (GunInHand >= 0 && gunMesh1.renderer) 
				gunMesh1.renderer.material = arse.Guns[(int)GunInHand].Mat;
			if (GunOnBack >= 0 && gunMesh2.renderer) 
				gunMesh2.renderer.material = arse.Guns[(int)GunOnBack].Mat;
		}
		
		// heads
		for (int i=0; i<heads.Length; i++) {
			if (i!=headType) {
				heads[i].renderer.enabled = false;
			}
			
			if (!visible) {
				heads[i].renderer.material = invisibleMat;
			}	
		}
		
		if (visible) {
			heads[0].renderer.material = newMatA;
			heads[1].renderer.material = boxMat;
			heads[2].renderer.material = fishMat;
			heads[3].renderer.material = bananaMat;
			heads[4].renderer.material = creeperMat;
			heads[5].renderer.material = elephantMat;
			heads[6].renderer.material = moonMat;
			heads[7].renderer.material = pyramidMat;
			heads[8].renderer.material = chocoboMat;
			heads[9].renderer.material = spikeMat;
			heads[10].renderer.material = tentacleMat;
			heads[11].renderer.material = robotHeadMat;
			heads[12].renderer.material = speaceshipMat;
			heads[13].renderer.material = enforcerMat;
			heads[14].renderer.material = smileyMat;
			heads[15].renderer.material = helmetMat;
			heads[16].renderer.material = paperbagMat;
			heads[17].renderer.material = maheadMat;
		}
		
		if (firstPersonGun != null && 
			User.local && 
			firstPersonGun.renderer && 
			GunInHand >= 0
		) {
			if (visible) {
				firstPersonGun.renderer.enabled = false;
			}else{
				firstPersonGun.renderer.enabled = true;
				firstPersonGun.renderer.material = arse.Guns[(int)GunInHand].Mat;
			}
		}
		
		if (!net.CurrMatch.pitchBlack || !User.local) {
			firstPersonLight.enabled = false;
		
		}
		
		if (!User.local && net.CurrMatch.pitchBlack) {
			if (net.CurrMatch.teamBased && User.team == net.localPlayer.team) {
				firstPersonLight.enabled = true;
				if (User.team == 1) {
					firstPersonLight.color = Color.red;
				}else{
					firstPersonLight.color = Color.cyan;
				}
			}
		}
	}
	
	public void ForceLook(Vector3 targetLookPos) {
		GameObject lookObj = new GameObject();
		lookObj.transform.position = Camera.main.transform.position;
		lookObj.transform.LookAt(targetLookPos, transform.up);
		lookObj.transform.parent = camHolder.transform.parent;
		camAngle = lookObj.transform.localEulerAngles;
		while (camAngle.x > 85f) 
			camAngle.x -= 180f;
		while (camAngle.x < -85f) 
			camAngle.x += 180f;
		//Debug.Log("Force look: " + targetLookPos.ToString() + " ??? " + lookObj.transform.position.ToString() + " ??? " + camAngle.ToString());
	}
	
	void FireBullet(Item weapon) {
		// fire hitscan type gun
		Vector3 bulletStart = Camera.main.transform.position;
		Vector3 bulletDirection = Camera.main.transform.forward;
		Vector3 bulletEnd = bulletStart + (bulletDirection*999f);
		bool hit = false;
		bool registerhit = false;
		int hitPlayer = -1;
	
		if (weapon == Item.MachineGun) {
			float shakeValue = 0.01f;
			bulletDirection += new Vector3(Random.Range(-shakeValue,shakeValue),Random.Range(-shakeValue,shakeValue),Random.Range(-shakeValue,shakeValue));
			bulletDirection.Normalize();
		}
				
		Ray bulletRay = new Ray(bulletStart, bulletDirection);
		RaycastHit bulletHit = new RaycastHit();
		int bulletLayer = (1<<0) | (1<<8); // walls & players
				
		gameObject.layer = 2;
		if (Physics.Raycast(bulletRay, out bulletHit, 999f, bulletLayer)) {
			bulletEnd = bulletHit.point;
					
			if (bulletHit.collider.gameObject.layer == 8) {
				// hit a player, tell the server
				hit = true;
				
				for (int i=0; i<net.players.Count; i++) {
					if (bulletHit.collider.gameObject == net.players[i].Entity.gameObject){
						hitPlayer = i;
					}
				}
			
				registerhit = true;
			}
		}
	
		gameObject.layer = 8;
		bulletStart = transform.position;
		bulletStart = gunMesh1.transform.position + (Camera.main.transform.forward*0.5f);
		// RPC the shot, regardless
		net.Shoot(weapon, bulletStart, bulletDirection, bulletEnd, net.localPlayer.viewID, hit);
	
		if (registerhit) 
			net.RegisterHit(weapon, net.localPlayer.viewID, net.players[hitPlayer].viewID, bulletHit.point);
	}
	
	public void Respawn() {
		Vector3 p = Vector3.up;
		Vector3 a = Vector3.zero;
		
		var so = GameObject.Find("_Spawns"); // player spawns object
		if (so != null) {
			var ss = so.GetComponent<SpawnPointScript>(); // spawns script
			
			if (!net.CurrMatch.teamBased) {
				int i = Random.Range(0, ss.normalSpawns.Length);
				p = ss.normalSpawns[i].transform.position + Vector3.up;
				a = ss.normalSpawns[i].transform.eulerAngles;
			}else if (User.team == 1) {
				int i = Random.Range(0, ss.team1Spawns.Length);
				p = ss.team1Spawns[i].transform.position + Vector3.up;
				a = ss.team1Spawns[i].transform.eulerAngles;
			}else if (User.team == 2) {
				int i = Random.Range(0, ss.team2Spawns.Length);
				p = ss.team2Spawns[i].transform.position + Vector3.up;
				a = ss.team2Spawns[i].transform.eulerAngles;
			}
		}
		
		transform.position = p;
		transform.LookAt(transform.position + Vector3.forward, Vector3.up);
		camAngle = a;
		yMove = 0f;
		moveVec = Vector3.zero;

		if (firstPersonGun) 
			Destroy(firstPersonGun);
		
		// assign spawn guns
		GunInHandCooldown = 0f;
		gunOnBackCooldown = 0f;
		GunInHand = net.CurrMatch.spawnGunA;
		GunOnBack = net.CurrMatch.spawnGunB;

		if (GunInHand == Item.Random)
			GunInHand = (Item)Random.Range(0, arse.Guns.Length);
		if (GunOnBack == Item.Random)
			GunOnBack = (Item)Random.Range(0, arse.Guns.Length);

		prevGunInHand = Item.None;
		prevGunOnBack = Item.None;
	}
	
	void LateUpdate() {
		if (User.health > 0f) {
			aimBone.transform.localEulerAngles += new Vector3(0, camAngle.x, 0);
			animObj.transform.localPosition = (animObj.transform.forward * camAngle.x * -0.002f) - Vector3.up;
		}
	}
	
	void NonLocalUpdate() {
		if (User.health <= 0f) 
			moveVec = Vector3.zero;
		
		if (cc == null) 
			cc = GetComponent<CharacterController>();
		if (ava == null) 
			ava = gameObject.AddComponent<Avatar>();
		
		float timeDelta = (float)(Network.time - lastUpdateTime);
		lastUpdateTime = Network.time;

		ava.UpVector = animObj.transform.up;
		
		if (crouched) {
			ava.Move(moveVec * timeDelta * 5f);
		}else{
			ava.Move(moveVec * timeDelta * 10f);
		}
		
		if (yMove <= 0f) {
			ava.Move(transform.up * -0.2f);
			grounded = ava.isGrounded;

			if (!grounded) 
				ava.Move(transform.up * 0.2f);
		}else{
			grounded = false;
		}
		
		if (grounded) {
			yMove = 0f;
		}else{
			yMove -= timeDelta * 10f;
		}
		
		ava.Move(transform.up * yMove * timeDelta * 5f);
	}
	
	public void UpdatePlayer(Vector3 pos, Vector3 ang, bool crouch, Vector3 move, float yMovement, double time, 
		Item gunA, Item gunB, Vector3 playerUp, Vector3 playerForward
	) {
		transform.position = pos;
		camHolder.transform.eulerAngles = ang;
		camAngle = ang;
		crouched = crouch;
		moveVec = move;
		yMove = yMovement;
		lastUpdateTime = time;
		GunInHand = gunA;
		GunOnBack = gunB;
		transform.LookAt(transform.position + playerForward ,playerUp);
		NonLocalUpdate();
	}
	
	public AudioClip sfx_takeDamage;
	public AudioClip sfx_jump;
	public AudioClip sfx_land;
	public AudioClip sfx_die;
	public AudioClip sfx_weaponChange;
	public AudioClip sfx_reload;
	public AudioClip sfx_swapped;
	public AudioClip sfx_catchBall;
	public AudioClip sfx_gravgun;
	public void PlaySound(UserAction action) {
		switch (action) {
			case UserAction.MoveUp:
				play(0.6f, sfx_jump);
				break;
			case UserAction.SwapWeapon:
				play(0.6f, sfx_weaponChange);
				break;
		}
	}
	public void PlaySound(string sound) {
		if (sound == "takeHit"){
			audio.clip = sfx_takeDamage;
			audio.Play();
		}
		if (sound == "jump"){
			play(0.2f, sfx_jump);
		}
		if (sound == "land"){
			audio.clip = sfx_land;
			audio.volume = 0.5f;
			audio.Play();
		}
		if (sound == "die"){
			audio.clip = sfx_die;
			audio.volume = 1f;
			audio.Play();
		}
		if (sound == "weaponChange"){
			play(0.2f, sfx_weaponChange);
		}
		if (sound == "reload"){
			audio.clip = sfx_reload;
			audio.volume = 0.2f;
			audio.Play();
		}
		if (sound == "Swapped"){
			audio.clip = sfx_swapped;
			audio.volume = 0.4f;
			audio.Play();
		}
		if (sound == "catchBall"){
			audio.clip = sfx_catchBall;
			audio.volume = 0.4f;
			audio.Play();
		}
		if (sound == "gravgun"){
			audio.clip = sfx_gravgun;
			audio.volume = 0.4f;
			audio.Play();
		}
	}
	
	void play(float volume, AudioClip clip) {
		audio.clip = clip;
		audio.volume = volume;
		audio.Play();
	}
}