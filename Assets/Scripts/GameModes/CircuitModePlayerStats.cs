﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Diese KLasse enthält Infos über den spieler, wie z.B. die Rundenzeit oder die anzahl an durchgefahrenen
 * Checkpoints. Sie speichert welchen Checkpoint das Auto zuletzt durchfahren hat.
 * Sie schaut auch, ob das Auto in die falsche Richtung fährt
 * Sie wird für jedes Fahrzeug benötigt, aber nur, wenn es sich um einen Rundkurs handelt (das macht der 
 * CircuitRaceMode).
 * Ursprünglich war sie nur ein Checkpoint Zähler.
 */

public class CircuitModePlayerStats : MonoBehaviour 
{
	//Referenz auf den LapRaceMode
	public CircuitRaceMode circuitMode;
	//nummer der Autos (wichtig bei mehreren Spielern)
	public int carNumber = -1;

	public int carIndex;

	public int currentPosition;
	
	//aktueller (zuletzt erfolgreich durchgefahrener) CheckPoint, in Fahrtrichtung
	private Checkpoint currentCheckpoint;
	//index des aktuellen Checkpoints
	public int currentCheckpointNumber;
	//die Anzahl der Checkpoints
	private int numberOfCheckpoints;
	//timer, der anspringen soll, wenn sich das Auto nach x sekunden in die falsche richtung fährt
	private float directionTimer = 0.0f;
	//wurde das rennen gestertet?
	private bool hasRaceStarted = false;
	//hat das Auto das Rennen beendet?
	private bool hasFinishedRace = false;
	//aktuelle Rundenzeit
	private float lapTime;
	//gesamtzeit
	private float totalTime;
	//schnellste Runde
	private float fastestLap;
	//anzahl durchgefahrerer Checkpoints
	public int numberOfDrivenCheckpoints = 0;
	//aktuell zu durchfahrende RUnde
	private int currentLapToDrive = 1;

	//soll die Differenz zur schnellsten Runde angezeigt werden?
	private bool showBestRound;
	//Timer, der anzeigt wie lange die beste Runde angezeigt werden soll
	private float bestRoundTimer;
	//Differenz der zur besten Zeit
	private float difference;
	//Fährt der Spieler in die falsche Richtung?
	private bool wrongWay = false;
	//Damit die Textur nicht dauerhaft da ist, sonder blinkt
	private float wrongWayTimer;

	public float networkTimeInitial;
	public float networkLapStartTime;

	void Start()
	{
		//this.networkView.viewID = Network.AllocateViewID();
		lapTime = 0.0f;
		totalTime = 0.0f;
		fastestLap = -1.0f;

		if(Network.connections.Length > 0){
			if(Network.isServer){
			   if(networkTimeInitial == 0.0f){
					GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
					for(int i = 0; i < players.Length; ++i){
						CircuitModePlayerStats stats = players[i].GetComponentInChildren<CircuitModePlayerStats>();
						stats.networkTimeInitial = (float)(Network.time);
						stats.networkLapStartTime = networkTimeInitial;
					}
				}
				this.networkView.RPC("transmitNetworkTime",RPCMode.OthersBuffered,networkTimeInitial);
			}
		}
	}

	[RPC]
	public void transmitNetworkTime(float time){
		networkTimeInitial = time;
		networkLapStartTime = networkTimeInitial;
	}



	// Update is called once per frame
	//hier wird überprüft, ob sich das AUto in der falschen Richtung bewegt
	//dazu wird geschaut, ob der Geschwindindigkeitsvektor mit der (Fahrt-)Richtung des Checkpoints übereinstimmt
	//außerdem werden die Rundenzeiten hochgezählt und das HUD aktuallisiert
	void Update () 
	{	
		//aktuallisiere die Rundenzeiten, aber nur, wenn Rennen gestartet und noch nicht beendet wurde
		if(hasRaceStarted == true && hasFinishedRace == false)
		{
			//zähle die Rundenzeit hoch,
			if(Network.connections.Length == 0){
				lapTime += Time.deltaTime;
				totalTime += Time.deltaTime;
			} else {
				lapTime = (float)(Network.time) - networkTimeInitial;
				totalTime = (float)(Network.time) - networkLapStartTime;
			}

			//überprüfe, ob das AUto in die falsche RIchtung fährt
			//falls wir in die richtige Richtung fahren, resete den Timer
			if (currentCheckpoint.isDrivingInRightDirection(transform.parent.GetComponent<Rigidbody>().velocity) == true)
			{
				directionTimer = 0.0f;
			}
			//ansonsten zähle den Timer hoch
			else
			{
				directionTimer += Time.deltaTime;
			}

			//schaue, ob das Auto bereits exploriert ist, wenn ja, beende das Rennen für diesen Fahrzeug
			Car car = transform.parent.GetComponent<Car>();
			if(car.getHealth() <= 0.0f)
			{
				hasFinishedRace = true;
				//beende das Rennen für diesen InputController
				PlayerInputController playerInput = transform.root.GetComponent<PlayerInputController>();
				playerInput.endRace();

				//übergebe die PlayerStats
				string fastestLapStr = "";
				//falls das AUto noch keine RUnde gefahren ist, stelle die schnelste Runde auf --:--:--
				if(fastestLap == -1.0f)
				{
					fastestLapStr = "--:--:--";
				}
				else
				{
					fastestLapStr = TimeConverter.floatToString(fastestLap);
				}
				//Spielername, zeige dsa das AUto zerstört wurde , schnellste Runde
				circuitMode.addExplodedPlayerData(new string[]{playerInput.playerName, "RIP", fastestLapStr});
				//sage dem HUD bescheid, dass das Rennen beendet wurde
				playerInput.hud.raceEnded = true;
			}

			//gucke, ob wir lange genug in die falsche Richtung fahren, um den HUD bescheid zu sagen
			if(directionTimer > 2.0f)
			{
				wrongWay = true;
			} else {
				wrongWay = false;
			}
			if(networkView.isMine || Network.connections.Length == 0){
				//HUD bescheidsagen, das er die Rundenzeit aktuellisieren soll
				//aber nur, wenn das Auto noch "lebt"
				if(car.getHealth() > 0.0f)
				{
					HUD hud = circuitMode.playerCtrl.playerList[carIndex].GetComponent<PlayerInputController>().hud;
					hud.modeInfo.text = "Lap " +currentLapToDrive+"/"+(circuitMode.lapsToDrive)+"\n"+ TimeConverter.floatToString(lapTime);
				}
			}
		}
	}

	//die Methode sagt dem Auto quasi Bescheid, dass das Rennen jetzt startet
	public void startRace()
	{
		hasRaceStarted = true;
	}
	
	//der erste Checkpoint wird gesetzt
	public void setFirstCheckpoint(Checkpoint chk)
	{
		currentCheckpoint = chk;
		currentCheckpointNumber = currentCheckpoint.checkpointNumber;
	}
	
	//die Anzahl der CHeckpoiints wird gesetzt
	public void setNumberOfCheckpoints(int chkNum)
	{
		numberOfCheckpoints = chkNum;
	}
	
	//die Methode gibt die Anzahl der durchgefahrenen Checkpoints zurück
	public int getNumberOfDrivenCheckpoints()
	{
		return numberOfDrivenCheckpoints;
	}
	
	//gibt die Nummer des momentanens Checkpoint zurück
	public int getCurrentCheckpointNumber()
	{
		return currentCheckpointNumber;
	}
	
	//liefert den aktuellen Checkpoint zurück
	public Checkpoint getCurrentCheckpoint()
	{
		return currentCheckpoint;
	}

	//liefert einen bool zurück, ob das Auto das Rennen bereits beendet hat oder nicht
	public bool getHasFinishedRace()
	{
		return hasFinishedRace;
	}

	//diese Methode wird aufgerufen, sobald eine Runde zu ende gefahren wurde
	private void finishedLap()
	{
		//zahle die Runde für dieses Auto hoch
		currentLapToDrive++;
		//fall es die erste Runde ist
		if(fastestLap == -1.0f)
		{
			fastestLap = lapTime;
			difference = lapTime;
		} else{
			difference = fastestLap-lapTime;
		}
		//aktuellisiere die schnellste Runde
		if(lapTime < fastestLap)
		{
			fastestLap = lapTime;
		}
		
		//falls es die letzt Runde war
		if(currentLapToDrive == circuitMode.lapsToDrive +1)
		{
			//zähle die RUndenzahl wieder ein runter, sonst steht nacher 3/2 dar, was nicht so gut aussieht
			currentLapToDrive--;

			//beende das Rennen für diesen InputController
			transform.parent.GetComponent<PlayerInputController>().endRace();
			
			//Setze den Throttle vom Auto auf 0 und bremse mit der Handbremse
			Car car = transform.parent.GetComponent<Car>();
			car.setThrottle(0.0f);
			car.setHandbrake(true);
			
			//das Rennden wurde beendet
			hasFinishedRace = true;
			wrongWay = false;

			//übergebe die PlayerStats
			//Controllernummer, gesamtzeit, schnellste RUnde
			circuitMode.playerHasFinishedRace(new string[]{transform.parent.GetComponent<PlayerInputController>().playerName, 
												TimeConverter.floatToString(totalTime), TimeConverter.floatToString(fastestLap)});
			//sage dem HUD bescheid, dass das Rennen beendet wurde
			if(this.networkView.isMine){
				gameObject.transform.parent.GetComponent<PlayerInputController>().hud.raceEnded = true;
			}
		}
		//ansonsten resete den Rundenzähler
		else 
		{
			lapTime = 0.0f;
			if(Network.connections.Length>0){
				networkLapStartTime = (float)Network.time;
			}
		}
		showBestRound = true;
	}

	public void endRaceBecauseHeIsTooSlow()
	{
		//beende das Rennen für diesen InputController
		transform.parent.GetComponent<PlayerInputController>().endRace();
		
		//Setze den Throttle vom Auto auf 0 und bremse mit der Handbremse
		Car car = transform.parent.GetComponent<Car>();
		car.setThrottle(0.0f);
		car.setHandbrake(true);
		
		//das Rennden wurde beendet
		hasFinishedRace = true;
		wrongWay = false;

		//übergebe die PlayerStats
		string fastestLapStr = "";
		//falls das AUto noch keine RUnde gefahren ist, stelle die schlesste Runde auf --:--:--
		if(fastestLap == -1.0f)
		{
			fastestLapStr = "--:--:--";
		}
		else
		{
			fastestLapStr = TimeConverter.floatToString(fastestLap);
		}

		//Controllernummer, war zu langsam, schnellste RUnde
		circuitMode.playerHasFinishedRace(new string[]{transform.parent.GetComponent<PlayerInputController>().playerName, "Too slow", fastestLapStr});
		//sage dem HUD bescheid, dass das Rennen beendet wurde
		if(this.networkView.isMine){
			gameObject.transform.parent.GetComponent<PlayerInputController>().hud.raceEnded = true;
		}
	}
	
	//diese Methode überprüft, ob das Auto den richtigen Checkpoint abgefahren hat
	public void updateCheckpoint(Checkpoint chkPoint)
	{
		//falls wir den richtigen Checkpoint abgefahren haben, setze den aktuellen Checkpoint
		if(currentCheckpoint.checkpointNumber + 1 == chkPoint.checkpointNumber)
		{
			currentCheckpoint = chkPoint;
			currentCheckpointNumber = currentCheckpoint.checkpointNumber;
			numberOfDrivenCheckpoints++;
		}
		//falls es die Checkpointmummer 0 ist (Startcheckpoint) und wir vom letzten gekommen sind
		else if(chkPoint.checkpointNumber == 0 && currentCheckpoint.checkpointNumber == numberOfCheckpoints - 1)
		{
			finishedLap();
			currentCheckpoint = chkPoint;
			currentCheckpointNumber = currentCheckpoint.checkpointNumber;
		}
	}

	//Zeichnet bei abgeschlossener Runde die Differenz zur bisher besten Rundenzeit an
	//Zeichnet außerdem die in Circuitmode zugewiesene Textur, wenn man in die falsche Richtung fährt
	void OnGUI(){
		//falls das Rennen für diesen Spieler noch nicht beendet wurde, zeige die Infos dar
		//ansonsten zeige nichts
		if(hasFinishedRace == false)
		{
			if(showBestRound){
				string differenceStr = TimeConverter.floatToString(difference);
				if(currentLapToDrive == 2){
					GUI.color = new Color(0,0,255);
					if(carNumber == 1)
						GUI.Label(new Rect(Screen.width/2-30,Screen.height*3/4-50,100,200), differenceStr);
					else if(carNumber == 0)
						GUI.Label(new Rect(Screen.width/2-30,Screen.height/4-50,100,200), differenceStr);
				} else {
					if(difference<0){
						GUI.color = new Color(0,255,0);
						if(carNumber == 1)
							GUI.Label(new Rect(Screen.width/2-30,Screen.height*3/4-50,100,200),"-" + differenceStr);
						else if(carNumber == 0)
							GUI.Label(new Rect(Screen.width/2-30,Screen.height/4-50,100,200),"-" + differenceStr);
					} else {
						GUI.color = new Color(255,0,0);
						if(carNumber == 1)
							GUI.Label(new Rect(Screen.width/2-30,Screen.height*3/4-50,100,200),"+" + differenceStr);
						else if(carNumber == 0)
							GUI.Label(new Rect(Screen.width/2-30,Screen.height/4-50,100,200),"+" + differenceStr);
					}
				}
				GUI.color = new Color(255,255,255);

				bestRoundTimer+= Time.deltaTime;
				if(bestRoundTimer>5.0f){
					showBestRound = false;
					bestRoundTimer = 0.0f;
				}
			}

			if(wrongWay && hasRaceStarted){
				float optimizedRatio = 1024 * 768;
				float currentRatio = Screen.width * Screen.height;
				float aspectRatio = currentRatio / optimizedRatio;
				if(wrongWayTimer >=1.0f){
					if(carNumber == 1){
						GUI.DrawTexture(new Rect(Screen.width/2 - 200*aspectRatio/2,Screen.height *3/4 - 200*aspectRatio/2,200*aspectRatio,200*aspectRatio),circuitMode.wrongWayTexture);
					}
					if(carNumber == 0){
						GUI.DrawTexture(new Rect(Screen.width/2 - 200*aspectRatio/2,Screen.height/4 - 200*aspectRatio/2,200*aspectRatio,200*aspectRatio),circuitMode.wrongWayTexture);
					}
					wrongWayTimer+=Time.deltaTime;
					if(wrongWayTimer>=2.0f){
						wrongWayTimer = 0.0f;
					}
				} else {
					wrongWayTimer+=Time.deltaTime;
				}
			}
		}
	}

	void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info){

		int currentCheckpointNumberSerial = -1;
		int currentPositionSerial = -1;
		int carNumberSerial = -1;
		int numberOfCheckpointsSerial = -1;
		float networkTimeInitialSerial = -1.0f;
		float networkLapTimeSerial = -1.0f;

		float lapTimeSerial = -1.0f;
		float totalTimeSerial = -1.0f;
		float fastestLapSerial = -1.0f;

		if(stream.isWriting){
			currentCheckpointNumberSerial = currentCheckpointNumber;
			stream.Serialize(ref currentCheckpointNumberSerial);

			currentPositionSerial = currentPosition;
			stream.Serialize(ref currentPositionSerial);

			carNumberSerial = carNumber;
			stream.Serialize(ref carNumberSerial);

			numberOfCheckpointsSerial = numberOfCheckpoints;
			stream.Serialize(ref numberOfCheckpointsSerial);

			networkTimeInitialSerial = networkTimeInitial;
			stream.Serialize(ref networkTimeInitialSerial);

			networkLapTimeSerial = networkLapStartTime;
			stream.Serialize(ref networkLapTimeSerial);

			lapTimeSerial = lapTime;
			stream.Serialize(ref lapTimeSerial);

			totalTimeSerial = totalTime;
			stream.Serialize(ref totalTimeSerial);

			fastestLapSerial = fastestLap;
			stream.Serialize(ref fastestLapSerial);
		} else {
			stream.Serialize(ref currentCheckpointNumberSerial);
			currentCheckpointNumber = currentCheckpointNumberSerial;

			stream.Serialize(ref currentPositionSerial);
			currentPosition = currentPositionSerial;

			stream.Serialize(ref carNumberSerial);
			carNumber = carNumberSerial;

			stream.Serialize(ref numberOfCheckpointsSerial);
			numberOfCheckpoints = numberOfCheckpointsSerial;

			stream.Serialize(ref networkTimeInitialSerial);
			networkTimeInitial = networkTimeInitialSerial;

			stream.Serialize(ref networkLapTimeSerial);
			networkLapStartTime = networkLapTimeSerial;

			stream.Serialize(ref lapTimeSerial);
			lapTime = lapTimeSerial;

			stream.Serialize(ref totalTimeSerial);
			totalTime = totalTimeSerial;

			stream.Serialize(ref fastestLapSerial);
			fastestLap = fastestLapSerial;
		}
	}

	[RPC]
	void setParent(NetworkViewID carID, NetworkViewID playerStatsID){
		if(this.networkView.viewID != playerStatsID){
			Debug.Log(playerStatsID);
			return;

		}

		foreach(GameObject g in GameObject.FindGameObjectsWithTag("Player")){
			if(g.networkView.viewID == carID){
				this.transform.parent = g.transform;
			}
		}

		circuitMode = GameObject.FindGameObjectWithTag("CircuitMode").GetComponent<CircuitRaceMode>();
		setFirstCheckpoint(circuitMode.firstCheckpoint);
	}
}