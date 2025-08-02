using UnityEngine;
using System.Collections.Generic;

public class SpawnCubes : MonoBehaviour {
	public GameObject wave;
	public GameManager gm;
	public Transform theWall;
	public List<GameObject> myWaves;
	public int length=20;
	private const float thethreeFourthLengthOfTheSprite = 0.713f;
	private const float startPositionForTheNextWave = 0.44f;
	int lastIndex =0;
	Vector2 lastWave= new Vector2();

	void Awake () {
		bool itHas3Holes = false;
		for (int i = 0; i < length; i++) {

			var f = Instantiate (wave) as GameObject;
			f.transform.SetParent (theWall);
			if(i<8){
				f.GetComponent<CubeWave> ().has5Cubes = true;
			}
			if (i < 3) {
				f.GetComponent<CubeWave> ().isStartingWaves = true;
			}
			if (i % 11 == 0 ) {
				f.GetComponent<CubeWave> ().has5Cubes = true;
			}

			if (itHas3Holes) {
				f.GetComponent<CubeWave> ().has5Cubes = true;
				itHas3Holes = false;
			}
			if (f.GetComponent<CubeWave> ().has3Holes == true) {
				itHas3Holes = true;
			}

			myWaves.Add (f);

			lastIndex++;
		}
	}


	void Start()
	{
		for (int i = 0; i < myWaves.Count; i++)
		{
			if (i % 2 != 0)
			{

				myWaves[i].transform.position = new Vector2(-startPositionForTheNextWave, i * -thethreeFourthLengthOfTheSprite);

			}
			else
			{
				myWaves[i].transform.position = new Vector2(0, i * -thethreeFourthLengthOfTheSprite);

			}
		}
	}

	public void CreateNewWave(){

		for (int i = 0; i < myWaves.Count; i++) {
			if (myWaves [i].activeInHierarchy == false) {
				myWaves [i].gameObject.SetActive (true);
				break;
			}
		}
		return;
	}

	public void InstantiateNewWave(){
		var f = Instantiate (wave);
		lastWave = myWaves [myWaves.Count-1].transform.position;
	
		if (lastIndex % 2 != 0) {
			f.transform.position = new Vector2 (-startPositionForTheNextWave,lastWave.y -thethreeFourthLengthOfTheSprite);
		} else {
			f.transform.position = new Vector2 (0,lastWave.y -thethreeFourthLengthOfTheSprite);
		}
	
		f.transform.SetParent (theWall);
		myWaves.Add (f);
		lastIndex++;
	}
		
}
