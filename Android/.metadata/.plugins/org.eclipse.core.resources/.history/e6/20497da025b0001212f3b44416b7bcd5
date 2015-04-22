package com.melvinius.homecontrol;

import android.os.Bundle;
import android.app.Activity;
import android.view.Menu;

import java.net.*;
import java.io.*;

public class Networking extends Activity {

    public Networking(int zoneNumber) {
    	try{ 
    	       String message = "system=sprinklers&action=demand&zone=" + zoneNumber + "&"; 
    	       int serverPortNum = 9099;    	       
    	       InetAddress serverAddress = InetAddress.getByName("311.homeftp.net");
    	       
    	       Socket clientSocket = new Socket(serverAddress, serverPortNum);
    	       PrintStream ps = new PrintStream(clientSocket.getOutputStream());
    	       ps.print(message);
    	       ps.flush();
    	       clientSocket.close();
    	 } 
    	 catch(Exception e){  
    	    	 e.printStackTrace(); 
    	 }
    }
    
}
