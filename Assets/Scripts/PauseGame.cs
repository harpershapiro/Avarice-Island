﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseGame : MonoBehaviour
{
	public bool gamePaused=false;
	public bool pausedForDialogue=false;
	public GameObject pauseMenu;
	public GameObject optionsMenu;
    public GameObject player;
    // Start is called before the first frame update
    void Start()
    {
    	pauseMenu.SetActive(false);
    	optionsMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
         if(Input.GetKeyDown(KeyCode.P)){
        	if(gamePaused==false){
        		Pause();
        	} else if (gamePaused==true){
        		Unpause();
        	}
        }

    }

    public void Pause(){
    	Time.timeScale=0f;
        gamePaused=true;
        player.GetComponent<Hero>().enabled=false;
        pauseMenu.SetActive(true);
    }

    public void Unpause(){
    	Time.timeScale=1f;
        gamePaused=false;
        player.GetComponent<Hero>().enabled=true;
        pauseMenu.SetActive(false);
    }

    //separate pause method for dialogue handling

    public void PauseForDialogue(){
		Time.timeScale=0f;
        pausedForDialogue=true;
        player.GetComponent<Hero>().enabled=false;
    }

    public void UnpauseForDialogue(){
    	Time.timeScale=1f;
        pausedForDialogue=false;
        player.GetComponent<Hero>().enabled=true;
    }

    public void RevealOptions(){
    	pauseMenu.SetActive(false);
    	optionsMenu.SetActive(true);
    }

    public void UnRevealOptions(){
    	pauseMenu.SetActive(true);
    	optionsMenu.SetActive(false);
    }
}
