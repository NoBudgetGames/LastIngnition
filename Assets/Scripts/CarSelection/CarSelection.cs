﻿using UnityEngine;
using System.Collections;

public class CarSelection : MonoBehaviour {

	public GameObject[] choosableCars;

	int currentSelectedCarIndex;
	GameObject currentSelectedCar;


	public string playerName = "One";

	bool controllerAttached;

	float selectionTimer;
	public bool playerReady;
	// Use this for initialization
	void Start () {
		currentSelectedCarIndex = 0;
		currentSelectedCar = (GameObject) GameObject.Instantiate(choosableCars[currentSelectedCarIndex],this.transform.position,this.transform.rotation);

		selectionTimer = 0.0f;

		for(int i = 0; i < Input.GetJoystickNames().Length; i++)
		{
			//überprüfe für Spieler One
			if(i == 0 && playerName.Equals("One") && Input.GetJoystickNames()[i] != null)
			{
				controllerAttached = true;
			}
			//überprüfe für Spieler Two
			if(i == 1 && playerName.Equals("Two") && Input.GetJoystickNames()[i] != null)
			{
				controllerAttached = true;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(selectionTimer > 0.0f){
			selectionTimer += Time.deltaTime;
			if(selectionTimer >= 1.0f)
				selectionTimer = 0.0f;
		}
		float axis;
		bool acceptButton;
		bool cancleButton;
		if(controllerAttached){
			axis = Input.GetAxis("Player" + playerName + "Steer");
			acceptButton = Input.GetButtonDown("Player" + playerName + "Fire");
			cancleButton = Input.GetButtonDown("Player" + playerName + "Handbrake");
		} else {
			axis = Input.GetAxis("Player" + playerName + "SteerKey");
			acceptButton = Input.GetButtonDown("Player" + playerName + "FireKey");
			cancleButton = Input.GetButtonDown("Player" + playerName + "HandbrakeKey");
		}
		if(selectionTimer == 0.0f && !playerReady){
			if(axis <= -0.5f){
				GameObject.Destroy(currentSelectedCar);
				if(currentSelectedCarIndex == 0)
					currentSelectedCarIndex = choosableCars.Length-1;
				else 
					currentSelectedCarIndex--;

				currentSelectedCar = (GameObject) GameObject.Instantiate(choosableCars[currentSelectedCarIndex],this.transform.position,currentSelectedCar.transform.rotation);
				selectionTimer += Time.deltaTime;
			} 
			if(axis >= 0.5f){
				GameObject.Destroy(currentSelectedCar);
				if(currentSelectedCarIndex == choosableCars.Length-1)
					currentSelectedCarIndex = 0;
				else 
					currentSelectedCarIndex++;
				
				currentSelectedCar = (GameObject) GameObject.Instantiate(choosableCars[currentSelectedCarIndex],this.transform.position,currentSelectedCar.transform.rotation);
				selectionTimer += Time.deltaTime;
			} 
		}
		if(acceptButton){
			playerReady = true;
			//finde die Netzwerk Objekt
			GameObject network = GameObject.Find("Network");
			if(network != null)
			{
				NetworkView netView = network.networkView;
				//aktuallisiert die PlayerData
				if(playerName.Equals("One"))
				{
					NetworkPlayerData data = GameObject.Find("playerDataOne").GetComponent<NetworkPlayerData>();
					data.setChoosenCar(choosableCars[currentSelectedCarIndex].GetComponent<Car>().carName);
					//update die PlayerInfos auf dem Server
					netView.RPC("updatePlayerInfo",RPCMode.Server, data.getPlayerData()[0], data.getPlayerData()[1], data.getPlayerData()[2], data.getPlayerData()[3]);
				}
				else
				{
					NetworkPlayerData data = GameObject.Find("playerDataTwo").GetComponent<NetworkPlayerData>();;
					data.setChoosenCar(choosableCars[currentSelectedCarIndex].GetComponent<Car>().carName);
					//update die PlayerInfos auf dem Server
					netView.RPC("updatePlayerInfo",RPCMode.Server, data.getPlayerData()[0], data.getPlayerData()[1], data.getPlayerData()[2], data.getPlayerData()[3]);
				}
				//bitte an der Server, die Infos für alle zu aktualliesieren
				netView.RPC("updatePlayersForClients",RPCMode.Server);
			}
		}
		if(cancleButton){
			if(!playerReady){
				Application.LoadLevel("MainMenuScene");
			} else{
				playerReady = false;
			}
		}
	}

	public int getCarTypeIndex(){
		return currentSelectedCarIndex;
	}

	public GameObject getSelectedCarObject(){
		return currentSelectedCar;
	}
}
