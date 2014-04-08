using UnityEngine;
using System.Collections;

public class RocketScript : MonoBehaviour {
	public GameObject particle;
	public int MinParticlesPerFrame = 6;
	public int MaxParticlesPerFrame = 10;
	public int ExplosionParticles = 50;
	public float ExplosionSize = 0.4f;
	public float ForwardOffset = 1f; //prevents running into your own rocket when you have lags
	public float Turn = 2f;
	public bool Turning = false;
	public float MinParticleSpeed = 5f;
	public float MaxParticleSpeed = 8f;
	public float PMR = 1f; //Particle Movement Randomness
	public float ExplosionSpeed = 8f;
	public NetworkViewID viewID;
	public NetworkViewID shooterID;
	
	private CcNet net;
	private Vector3 lastPos;
	private float life = 20f;
	
	
	
	void Start () {
		net = GameObject.Find("Main Program").GetComponent<CcNet>();
		lastPos = transform.position;
		transform.position += transform.forward * ForwardOffset;
	}
	
	void Update () {
		if (enabled){
			if (Turning)
				transform.forward = Vector3.Normalize(transform.forward + Random.insideUnitSphere * Time.deltaTime * Turn);
			Vector3 moveForward = transform.forward * Time.deltaTime * 600f / (life < 16f ? 1f : Mathf.Pow(life - 15f, 2f));
			transform.position += moveForward;
			int c_pts = Random.Range(MinParticlesPerFrame, MaxParticlesPerFrame);
			if(Turning)
				for (int i = 0; i < c_pts; i++) {
					var np = (GameObject)GameObject.Instantiate(particle);
					float offMagn = moveForward.magnitude / 2f;
					Vector3 offset = new Vector3(Random.Range(-offMagn, offMagn), Random.Range(-offMagn, offMagn), Random.Range(-offMagn, offMagn));
					np.transform.position = transform.position + offset;
					np.GetComponent<BeamParticle>().MoveVec = -transform.forward * Random.Range(MinParticleSpeed, MaxParticleSpeed) + Random.insideUnitSphere * PMR;
					np.GetComponent<BeamParticle>().life = 0.5f;
					np.GetComponent<BeamParticle>().StartColor = Color.Lerp(Color.green, Color.blue, Random.value);
					np.GetComponent<BeamParticle>().UseMidColor = true;
					np.GetComponent<BeamParticle>().MidColor = Color.Lerp(Color.green, Color.blue, Random.value);
					np.GetComponent<BeamParticle>().MidColorPos = Random.Range(0.4f, 0.5f);
					np.GetComponent<BeamParticle>().EndColor = Color.clear;
				}
			else
				for (int i = 0; i < c_pts; i++) {
					var np = (GameObject)GameObject.Instantiate(particle);
					float offMagn = moveForward.magnitude / 2f;
					Vector3 offset = new Vector3(Random.Range(-offMagn, offMagn), Random.Range(-offMagn, offMagn), Random.Range(-offMagn, offMagn));
					np.transform.position = transform.position + offset;
					np.GetComponent<BeamParticle>().MoveVec = -transform.forward * Random.Range(MinParticleSpeed, MaxParticleSpeed) + Random.insideUnitSphere * PMR;
					np.GetComponent<BeamParticle>().life = 0.5f;
					np.GetComponent<BeamParticle>().StartColor = Color.Lerp(Color.white, Color.yellow, Random.value);
					np.GetComponent<BeamParticle>().UseMidColor = true;
					np.GetComponent<BeamParticle>().MidColor = Color.Lerp(Color.red, Color.black, Random.Range(0f, 0.5f));
					np.GetComponent<BeamParticle>().MidColorPos = Random.Range(0.4f, 0.6f);
					np.GetComponent<BeamParticle>().EndColor = Color.clear;
				}
			var hitInfo = new RaycastHit();
			int layerMask = (1<<8) | 1;   //(1<<0);
			var rayDirection = (transform.position - lastPos).normalized;
			if (Physics.SphereCast(lastPos, 0.15f, rayDirection, out hitInfo, Vector3.Distance(transform.position, lastPos), layerMask)) {
				//Debug.Log(hitInfo.collider.gameObject.name);
				detonateMaybe();
			}
			
			lastPos = transform.position;
			
			life -= Time.deltaTime;
			if (life <= 0f) {
				detonateMaybe();
			}
		}
	}
	
	private void detonateMaybe() {
		if (Turning)
		for (int i = 0; i < ExplosionParticles; i++) {
			var np = (GameObject)GameObject.Instantiate(particle);
			np.transform.position = Random.insideUnitSphere * ExplosionSize + lastPos;
			np.GetComponent<BeamParticle>().MaxSpeed = ExplosionSpeed;
			np.GetComponent<BeamParticle>().StartColor = Color.Lerp (Color.green, Color.yellow, Random.value);
			np.GetComponent<BeamParticle>().UseMidColor = true;
			np.GetComponent<BeamParticle>().MidColor = Color.Lerp (Color.green, Color.blue, Random.value);
			np.GetComponent<BeamParticle>().MidColorPos = 0.7f;
			np.GetComponent<BeamParticle>().EndColor = Color.clear;
			np.GetComponent<BeamParticle>().life = 0.5f;
			np.GetComponent<BeamParticle>().f = 1f;
			np.GetComponent<BeamParticle>().MinSize = 6f;
			np.GetComponent<BeamParticle>().MaxSize = 8f;
			np.GetComponent<BeamParticle>().acceleration = 0.1f;
		}
		else
		for (int i = 0; i < ExplosionParticles; i++) {
			var np = (GameObject)GameObject.Instantiate(particle);
			np.transform.position = Random.insideUnitSphere * ExplosionSize + lastPos;
			np.GetComponent<BeamParticle>().MaxSpeed = ExplosionSpeed;
			np.GetComponent<BeamParticle>().StartColor = Color.Lerp(Color.red, Color.yellow, Random.value);
			np.GetComponent<BeamParticle>().EndColor = Color.Lerp(Color.Lerp(Color.red, Color.clear, 1f), Color.Lerp(Color.black, Color.clear, 1f), Random.Range(0f, 1f));
			np.GetComponent<BeamParticle>().life = 0.5f;
			np.GetComponent<BeamParticle>().f = 1f;
			np.GetComponent<BeamParticle>().MinSize = 6f;
			np.GetComponent<BeamParticle>().MaxSize = 8f;
			np.GetComponent<BeamParticle>().acceleration = 0.1f;
		}
		enabled = false;
		if (net.isServer)
			net.Detonate(Item.RocketProjectile, transform.position, shooterID, viewID);
	}
}
