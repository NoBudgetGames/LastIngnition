﻿using System;

/* Dieser Enum enthält Informationen, aus welcher Richtung der Schaden relativ zum Fahrzeug kam.
 * Der Wert selber wird vom Trigger am Fahrzeug benutzt, um im Car Script die applyDamage Methode aufzurufen
 */ 

public enum DamageDirection
{
	FRONT, 
	REAR, 
	LEFT, 
	RIGHT
};

