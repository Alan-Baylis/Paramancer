using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Arsenal : MonoBehaviour {
	public int WidestIcon;
	public int TallestIcon;
	public GunData[] Guns;
	
	// private 
	CcNet net;
	List<Grenade> activeGrenades = new List<Grenade>();
	List<Rocket> activeRockets = new List<Rocket>();
	
	
	
	void Start() {
		net = GetComponent<CcNet>();

		// setup guns 
		Guns = new GunData[(int)Gun.Count];

		string s = "";
		for (int i = 0; i < Guns.Length; i++) {
			Guns[i] = new GunData();

			var n = S.GetSpacedOut("" + (Gun)i);
			Guns[i].Name = n;

			switch ((Gun)i) {
				default:
					Guns[i].Prefab = (GameObject)Resources.Load("Item/Weap/Gun/" + n + "/" + n + " PREFAB");
					Guns[i].Mat =      Resources.Load<Material>("Item/Weap/Gun/" + n + "/" + n); 
					Guns[i].Pic =      Resources.Load<Texture> ("Item/Weap/Gun/" + n + "/" + n); 
					break;
			}

			s += n + ",  ";
			
			// set widest icon 
			int w = Guns[i].Pic.width;
			if (WidestIcon < w) 
				WidestIcon = w;
			
			// set tallest icon 
			int h = Guns[i].Pic.height;
			if (TallestIcon < h) 
				TallestIcon = h;
		}

		// pics   (FOR NOW, cycle by id/index into an alphabetized LoadAll() array 
		for (int i = 0; i < Guns.Length; i++) {
			switch ((Gun)i) {
				case Gun.Pistol:   Guns[i].Color = Color.white; 
					Guns[i].Delay = 0.3f; 
					Guns[i].DelayAlt = 0.3f; 
					break; 
				case Gun.GrenadeLauncher:   Guns[i].Color = Color.green; 
					Guns[i].Delay = 0.25f; 
					Guns[i].DelayAlt = 0.25f;
					Guns[i].BlastRadius = 4f; // BR 
					break; 
				case Gun.MachineGun:   Guns[i].Color = Color.yellow; 
					Guns[i].Delay = 0.1f; 
					Guns[i].DelayAlt = 0.1f; Guns[i].AutoFire = true; // only 1 with AutoFire 
					break; 
				case Gun.RailGun:   Guns[i].Color = Color.cyan; 
					Guns[i].Delay = 2f;
					Guns[i].MarkScale = 2f; // 
					Guns[i].DelayAlt = 2f;
					break;
				case Gun.RocketLauncher:   Guns[i].Color = Color.red; 
					Guns[i].Delay = 1.5f; 
					Guns[i].DelayAlt = 0.7f;
					Guns[i].MarkScale = 5f; // set for the launcher because the projectile has a negative value in the gun system 
					Guns[i].BlastRadius = 4f; // BR 
					break;
				case Gun.Swapper:   Guns[i].Color = Color.magenta; 
					Guns[i].Delay = 2f; 
					Guns[i].DelayAlt = 2f; 
					break; 
				case Gun.Gravulator:   Guns[i].Color = Color.Lerp(Color.red, Color.yellow, 0.5f); 
					Guns[i].Delay = 1f; 
					Guns[i].DelayAlt = 1f; 
					break; 
				case Gun.Bomb:   Guns[i].Color = Color.red; 
					Guns[i].Delay = 1f; 
					Guns[i].DelayAlt = 1f; 
					Guns[i].BlastRadius = 10f; // BR 
					break; 
			case Gun.Spatula:   Guns[i].Color = Color.gray; 
					Guns[i].Delay = 1f;  
					Guns[i].DelayAlt = 1f;
					Guns[i].Range = 3f; // only 1 with Range 
					break; 
			}
		}
	}
	
	public void Clear() {
		// FIXME?  should we be clearing out rockets here too? if not, change method name to be more specific, since its not a Clear'ing of the whole Arsenal system 
		for (int i=0; i<activeGrenades.Count; i++) {
			if (activeGrenades[i] != null && activeGrenades[i].gameObject != null) 
				Destroy(activeGrenades[i].gameObject);
		}
		
		activeGrenades = new List<Grenade>();
	}

	void shootSwapper (Vector3 origin, Vector3 end, NetworkViewID shooterID, bool hit) {
		bool localFire = false;
		Vector3 localstart = origin;
		for (int i=0; i<net.players.Count; i++){
			if (net.players[i].viewID == shooterID && net.players[i].local) {
				localFire = true;
				localstart = net.players[i].Entity.HudGun.transform.position + (Camera.main.transform.forward*0.5f);
			}
		}
		
		var lbp = (GameObject)GameObject.Instantiate(GOs.Get("Lightning"));
		var lb = lbp.GetComponent<Lightning>();

		if (localFire && !hit) 
			lb.start = localstart;
		else
			lb.start = origin;

		lb.end = end;
		lb.hit = hit;
	}

	void shootHitscan(Vector3 origin, Vector3 end, NetworkViewID shooterID, Gun weapon, Vector3 hitNorm) {
		bool localFire = false;
		Vector3 localStart = origin;

		for (int i=0; i<net.players.Count; i++) {
			// if local player 
			if (net.players[i].viewID == shooterID && net.players[i].local){
				localFire = true;
				localStart = net.players[i].Entity.HudGun.transform.position + (Camera.main.transform.forward * 0.5f);
			}
		}

		if (hitNorm != Vector3.zero) {
			// bullet mark 
			GameObject nh = (GameObject)GameObject.Instantiate(GOs.Get("BulletMark"));
			nh.transform.position = end + hitNorm * 0.01f;
			nh.transform.forward = -hitNorm;
			nh.transform.localScale *= Guns[(int)weapon].MarkScale;
			nh.GetComponent<BulletMark>().StartCol = Color.Lerp(Color.gray, Color.black, Random.value);
			nh.GetComponent<BulletMark>().MaxLife = 10f;

			// particles 
			for (int i = 0; i < 100; i++) {
				Vector3 diagonalVec = Quaternion.Euler(Random.Range(-30f, 30f), Random.Range(-30f, 30f), Random.Range(-30f, 30f)) * hitNorm;
				var np = (GameObject)GameObject.Instantiate(GOs.Get("CcParticle"));
				np.transform.position = end + diagonalVec * Random.Range(0.1f, 0.3f);
				var p = np.GetComponent<CcParticle>();
				p.MoveVec = diagonalVec * Random.Range(2f, 3f);
				p.MinSize = 0.3f;
				p.MaxSize = 0.4f;
				p.StartColor = Guns[(int)weapon].Color;
				p.EndColor = Color.clear;
				p.ParticType = ParticleType.Puff;
				p.life = Random.Range(0.65f, 0.75f);
			}
		}
		
		if (weapon == Gun.Spatula)
			return; // (no trail effects) 
		
		// fx trail 
		var tj = (GameObject)GameObject.Instantiate(GOs.Get("TrailJagged"));
		var b = tj.GetComponent<TrailStraight>();

		if (localFire) 
			b.Begin = localStart;
		else
			b.Begin = origin;

		b.End = end; // - Vector3.Normalize(b.End - b.Begin) * 0.3f; // so that the trail seems to enter the wall instead of having a rectangular ending 
		b.Color = Guns[(int)weapon].Color;

		// muzzle flash 
		var mf = (GameObject)GameObject.Instantiate(GOs.Get("MuzzleFlash"));
		mf.light.color = Guns[(int)weapon].Color;
		mf.transform.position = origin;
		if (localFire) { // Sophie didn't allow remote player flashes? 
			mf.transform.position = localStart - (Camera.main.transform.right * 0.2f);
		}
		
		// rail spiral 
		if (weapon == Gun.RailGun) {
			Vector3 beamStart = origin;
			if (localFire) 
				beamStart = localStart;

			Vector3 beamDir = (end-beamStart).normalized;
			float maxLen = Vector3.Distance(end, beamStart);
			if (maxLen > 160f) 
				maxLen = 160f;

			float angle = 0f;
			float progress = 0f;
			while (progress < maxLen) {
				var np = (GameObject)GameObject.Instantiate(GOs.Get("CcParticle"));
				var v = Camera.main.transform.up / 4f;
				v = Quaternion.AngleAxis(angle, Camera.main.transform.forward) * v;
				var center = beamStart + (beamDir * progress);
				np.transform.position = center + v;
				var p = np.GetComponent<CcParticle>();
				p.MoveVec = Quaternion.AngleAxis(90f, Camera.main.transform.forward) * v * 2f;
				p.MinSize = 0.4f;
				p.MaxSize = 0.4f;
				p.StartColor = Color.blue;
				p.EndColor = Color.clear;
				p.ParticType = ParticleType.Circle;
				progress += 0.20f;
				angle += 24f;
			}
		}
	}

	public void Shoot(Gun weapon, Vector3 origin, Vector3 direction, Vector3 end, 
		NetworkViewID shooterID, NetworkViewID bulletID, double time, bool hit, bool alt, Vector3 hitNorm, bool sprint = false
	) {
		switch (weapon) {
			case Gun.Pistol:
			case Gun.MachineGun:
			case Gun.RailGun:
			case Gun.Spatula:
				shootHitscan(origin, end, shooterID, weapon, hitNorm);
				break;
		
			case Gun.Swapper:
				shootSwapper(origin, end, shooterID, hit);
				break;
			
			case Gun.GrenadeLauncher:
				var ng = (GameObject)GameObject.Instantiate(GOs.Get("Grenade"));
				var g = ng.GetComponent<Grenade>();
				g.ThrowerPos = origin;
				g.direction = direction;
				g.startTime = time;
				g.viewID = bulletID;
				g.shooterID = shooterID;
				g.detonationTime = 3f;
				
				activeGrenades.Add(g);
				break;
			
			case Gun.RocketLauncher:
				var nr = (GameObject)GameObject.Instantiate(GOs.Get("Rocket"));
				nr.transform.position = origin + direction; // start a bit outwards 
				nr.transform.LookAt(origin + direction * 2f);
				
				var	rs = nr.GetComponent<Rocket>();
				rs.viewID = bulletID;
				rs.shooterID = shooterID;
				
				activeRockets.Add(nr.GetComponent<Rocket>());

				if (alt)
					rs.Turning = true;
				break;
		}
		
		// play sound 
		for (int i=0; i<net.players.Count; i++) {
			if (net.players[i].viewID == shooterID) {
				switch (weapon) {
					// case Item.Gravulator: 
					// *** the activation sound is currently located along with jump/land sfx. //FIXME??? 
					// it's not sending the shot sound trigger over the net. 
					// so a pursuer has to look around for a flee'er (since you can't hear them) 
					// when they enter into a large space/room.  this may be a good thing? 
					case Gun.GrenadeLauncher:  playPitchedSfx(i, Sfx.Get("boosh")); break;
					case Gun.RocketLauncher:   playPitchedSfx(i, Sfx.Get("shot_bazooka")); break;
					default: playPitchedSfx(i, Sfx.Get(weapon.ToString())); break;
				}
			}
		}
	}
		
	void playPitchedSfx(int i, AudioClip ac) { // randomly pitched for variety 
		net.players[i].Entity.weaponSoundObj.audio.clip = ac;
		
		// if local user 
		if (net.players[i].viewID == net.localPlayer.viewID) 
			net.players[i].Entity.weaponSoundObj.audio.volume = 0.3f;
		
		net.players[i].Entity.weaponSoundObj.audio.pitch = Random.Range(0.9f, 1.1f);
		net.players[i].Entity.weaponSoundObj.audio.Play();
	}
	
	public float GetWeaponDamage(Gun weapon) {
		switch (weapon) {
			case Gun.Pistol:         
				return 40f;
			case Gun.GrenadeLauncher:        
				return 60f;
			case Gun.MachineGun:     
				return 15f;
			case Gun.Spatula:          
				return 105f;
			case Gun.RailGun:          
				return 105f;
			case Gun.RocketLauncher: 
				return 70f;
			
			case Gun.Lava:           
				return 9999f;
			case Gun.Bomb:           
				return 9999f;
			case Gun.Suicide:        
				return 9999f;
		}

		return 0f;
	}
	
	// FIXME: pull BombBeep(), and the bomb related part of Detonate(), into a script in the bomb's folder 
	public void BombBeep(Vector3 pos) {
		var o = (GameObject)GameObject.Instantiate(GOs.Get("WeapSound")); // bomb beep/sound object 
		o.transform.position = pos;
		o.audio.clip = Sfx.Get("BombBeep");
		o.audio.volume = 1f;
	}
	
	public void Detonate(Gun weapon, Vector3 detPos, NetworkViewID viewID) {
		if (weapon == Gun.Bomb) {
			var o = (GameObject)GameObject.Instantiate(GOs.Get("SphereExplosion"));
			o.transform.position = detPos;
			o.GetComponent<SphereExplosion>().Color = Color.red;
			o.GetComponent<SphereExplosion>().MaxRadius = Guns[(int)weapon].BlastRadius;

			var ws = (GameObject)GameObject.Instantiate(GOs.Get("WeapSound"));
			ws.transform.position = detPos;
			ws.audio.clip = Sfx.Get("ExplodeBomb");
			ws.audio.volume = 4f;
		} else if (weapon == Gun.GrenadeLauncher) {
			for (int i=0; i<activeGrenades.Count; i++) {
				if (viewID == activeGrenades[i].viewID) {
					
					var o = (GameObject)GameObject.Instantiate(GOs.Get("SphereExplosion"));
					o.transform.position = activeGrenades[i].transform.position;
					o.GetComponent<SphereExplosion>().MaxRadius = Guns[(int)weapon].BlastRadius;
					o.GetComponent<SphereExplosion>().Color = new Color(0.705f, 1f, 0f); // chartreuse for green grenades 
					
					var nade = (GameObject)GameObject.Instantiate(GOs.Get("WeapSound"));
					nade.transform.position = activeGrenades[i].transform.position;
					nade.audio.clip = Sfx.Get("ExplodeGrenade");
					nade.audio.volume = 2f;
					
					Destroy(activeGrenades[i].gameObject);
					activeGrenades.RemoveAt(i);
				}
			}
		}else{
			print ("WARNING: Detonate() was called with gun: " + weapon + "!!!!");
		}
	}

	public void DetonateRocket(Vector3 detPos, Vector3 hitNorm, NetworkViewID viewID) {
		for (int i=0; i<activeRockets.Count; i++) {
			// if this is the right rocket 
			if (viewID == activeRockets[i].viewID) {
				var rPos = activeRockets[i].transform.position;

				// rocket jumping 
				// 		look for self in user list 
				for (int k=0; k<net.players.Count; k++) {
					var user = net.players[k];

					if (user.local) {
						var entPos = user.Entity.transform.position;

						if (Vector3.Distance(entPos, rPos)
						    <
						    Guns[(int)Gun.RocketLauncher].BlastRadius
					    ) {
							// if user higher than the rocket 
							if (entPos.y > rPos.y) {
								// if we were the shooter of this rocket 
								if (activeRockets[i].shooterID == user.viewID){
									user.Entity.yMove = 5.2f;
								}else{ // for now, we never actually bounce remote players 
									user.Entity.yMove = 2f;
								}

								user.Entity.grounded = false;
								user.Entity.sendRPCUpdate = true;
							}
						}
					}
				}
				
				// detonate rocket 
				// 		sound 
				var ws = (GameObject)GameObject.Instantiate(GOs.Get("WeapSound"));
				ws.transform.position = rPos;
				ws.audio.clip = Sfx.Get("explosion_bazooka");
				ws.audio.volume = 9f; // hmmm, the docs said range from 0f - 1f i believe 
				// 		blast visual 
				var splo = (GameObject)GameObject.Instantiate(GOs.Get("SphereExplosion"));
				splo.transform.position = rPos;
				splo.GetComponent<SphereExplosion>().Color = S.Orange;
				splo.GetComponent<SphereExplosion>().MaxRadius = Guns[(int)Gun.RocketLauncher].BlastRadius;
				//		cleanup 
				Destroy(activeRockets[i].gameObject);
				activeRockets.RemoveAt(i);
				// 		bullet marks 
				if (hitNorm != Vector3.zero) {
					var o = (GameObject)GameObject.Instantiate(GOs.Get("BulletMark"));
					o.transform.position = detPos + hitNorm * 0.03f;
					o.transform.forward = -hitNorm;
					o.transform.localScale *= Guns[(int)Gun.RocketLauncher].MarkScale;
					o.GetComponent<BulletMark>().StartCol = Color.Lerp(Color.gray, Color.black, Random.value);
					o.GetComponent<BulletMark>().MaxLife = 30f;
				}
			}
		}
	}
}