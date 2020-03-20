﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinibossSpawnPoint : MonoBehaviour
{
	
	public GameObject spawnPrefab; //should be a miniboss
	public int spawnDistFromPlayer;
	public Dialogue entranceDialogue;

    public Vector3 spawnLocation, playerLocation;
    private bool spawnTriggered;
    private bool okToSpawn;

    private GameObject player;
  	private GameObject miniBoss;

  	//camera vars. could reorganize this
    private GameManager gameManager;
    private CameraBounds cameraBounds;
    private Transform currentCameraTrans;
    private bool panToMiniboss;
    private bool reachedLimit;
    private float distPanned;
    public float distToPan; //defines how far the camera should pan
    public float panInterval = 0.08f;




    // Start is called before the first frame update
    void Start()
    {
        spawnTriggered=false;
        panToMiniboss=false;
        reachedLimit=false;
        distPanned=0f;
        player = GameObject.Find("Player");
        gameManager = GameObject.Find("MyGameManager").GetComponent<GameManager>();
        spawnLocation = gameObject.transform.position;
        Debug.Log("Spawn location: " + gameObject.transform.position);
        playerLocation = player.transform.position;
        //cameras
        cameraBounds = gameManager.cameraBounds; 
        currentCameraTrans = cameraBounds.cameraRoot;
    }

    // Update is called once per frame
    void Update()
    {
         //wait until player is in range to start spawning
        playerLocation = player.transform.position;
    	if(spawnLocation.x-playerLocation.x < spawnDistFromPlayer && !spawnTriggered){
        	spawnTriggered = true; //can probably forgo this system by destroying the spawn point
        	okToSpawn = true;
        }

        //NEED TO REFACTOR PANNING SOMEWHERE ELSE maybe just game manager
        if(spawnTriggered && okToSpawn){
        	Debug.Log("about to spawn han");
        	okToSpawn = false;

        	//cinematic pan vars
        	panToMiniboss = true;
        	gameManager.LockCamera();
        	//must freeze player. all grunts should be dead though.
        	player.GetComponent<Hero>().enabled=false;



         //if(okToSpawn()){
        	miniBoss = Instantiate(spawnPrefab,spawnLocation,Quaternion.identity);
        }

        //do a cinematic pan to han lao as he spawns
        if(panToMiniboss && distPanned<=distToPan){
        	Debug.Log("trying to pan");
        	cameraBounds.SetXPosition(currentCameraTrans.position.x + panInterval);
        	distPanned+=panInterval;
        	//condition: camera has panned full distance
        	//if(System.Math.Abs(currentCameraTrans.position.x - miniBoss.transform.position.x) < 0.2){
        } else {
        	panToMiniboss = false;
        	gameManager.UnlockCamera();
        	player.GetComponent<Hero>().enabled=true;
            //this may not work as expected
        }

    }

    /*
    public void PanToMiniboss(){
    	//take control of the camera, pan, and return
    	gameManager.LockCamera();

    	return;
    }*/


}
