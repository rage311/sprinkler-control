package com.melvinius.homecontrol;

import android.os.Bundle;
import android.app.Activity;
import android.view.Menu;
import android.view.View.OnClickListener;
import android.view.View;
import android.widget.Toast;
import android.util.Log;
import android.widget.Button;

import java.net.*;
import java.io.*;

public class SprinklerActivity extends Activity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_sprinkler);
        
        Log.v("onCreate", "werd werd 123");
        Toast.makeText(getApplicationContext(), "werd", Toast.LENGTH_LONG).show();
        
        //Networking netConn = new Networking();
        Button zone1Button = (Button)findViewById(R.id.zone1Button);
        zone1Button.setOnClickListener(zoneButtonListener);
        
        Button zone2Button = (Button)findViewById(R.id.zone2Button);
        zone2Button.setOnClickListener(zoneButtonListener);
        
        Button zone3Button = (Button)findViewById(R.id.zone3Button);
        zone3Button.setOnClickListener(zoneButtonListener);
        
        Button zone4Button = (Button)findViewById(R.id.zone4Button);
        zone4Button.setOnClickListener(zoneButtonListener);
        
        Button zone5Button = (Button)findViewById(R.id.zone5Button);
        zone5Button.setOnClickListener(zoneButtonListener);
        
        Button zone6Button = (Button)findViewById(R.id.zone6Button);
        zone6Button.setOnClickListener(zoneButtonListener);
        
        Button zone7Button = (Button)findViewById(R.id.zone7Button);
        zone7Button.setOnClickListener(zoneButtonListener);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.activity_sprinkler, menu);
        return true;
    }
    
    public View.OnClickListener zoneButtonListener = new OnClickListener() {

		@Override
		public void onClick(View v) {
			// TODO Auto-generated method stub
			if (v.getId() == R.id.zone1Button) {
				Networking netConn = new Networking(0);
			}
			else if (v.getId() == R.id.zone2Button) {
				Networking netConn = new Networking(1);
			}
			else if (v.getId() == R.id.zone3Button) {
				Networking netConn = new Networking(2);
			}
			else if (v.getId() == R.id.zone4Button) {
				Networking netConn = new Networking(3);
			}
			else if (v.getId() == R.id.zone5Button) {
				Networking netConn = new Networking(4);
			}
			else if (v.getId() == R.id.zone6Button) {
				Networking netConn = new Networking(5);
			}
			else if (v.getId() == R.id.zone7Button) {
				Networking netConn = new Networking(6);
			}
			
		}

    };
    
}
