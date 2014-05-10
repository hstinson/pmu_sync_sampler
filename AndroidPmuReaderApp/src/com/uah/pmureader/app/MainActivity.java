package com.uah.pmureader.app;

import com.uah.pmureader.PmuReaderService;
import com.uah.pmureader.R;
import com.uah.pmureader.WifiService;

import android.os.Bundle;
import android.app.Activity;
import android.content.Intent;
import android.util.Log;
import android.view.Menu;
import android.view.View;
import android.widget.EditText;

public class MainActivity extends Activity
{
	@Override
	protected void onResume() {
		super.onResume();
	}


	@Override
	protected void onCreate(Bundle savedInstanceState)
	{
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
	}
	

	@Override
	public boolean onCreateOptionsMenu(Menu menu)
	{
		// Inflate the menu; this adds items to the action bar if it is present.
		getMenuInflater().inflate(R.menu.main, menu);
		return true;
	}
	
	/**
	 * Starts the PMU reader service.
	 * @param view
	 */
	public void startService(View view)
	{
		Log.d("PERF APP", "Click Occured");
		Intent intent = new Intent(getBaseContext(), PmuReaderService.class);
		
		intent.putExtra("period", getTextFromTextBox(R.id.editTextPeriod, false));
		intent.putExtra("pmuEvent0", getTextFromTextBox(R.id.editText1, true));
		intent.putExtra("pmuEvent1", getTextFromTextBox(R.id.editText2, true));
		intent.putExtra("pmuEvent2", getTextFromTextBox(R.id.editText3, true));
		intent.putExtra("pmuEvent3", getTextFromTextBox(R.id.editText4, true));
		intent.putExtra("pmuEvent4", getTextFromTextBox(R.id.editText5, true));
		intent.putExtra("pmuEvent5", getTextFromTextBox(R.id.editText6, true));
		
		startService(intent);		
	}
	
	/**
	 * Stops the PMU reader service.
	 * @param view
	 */
	public void stopService(View view)
	{
		stopService(new Intent(getBaseContext(), PmuReaderService.class));
	}
	
	/**
	 * Configures and enables WiFi
	 * @param view
	 */
	public void configWifi(View view)
	{
		Intent intent = new Intent(getBaseContext(), WifiService.class);		
		intent.putExtra("ssid", "stinet3");
		intent.putExtra("pass", "useDefault");
		
		startService(intent);	
	}
	
	private int getTextFromTextBox(int editTextId, boolean isHexInput)
	{
		int rv = 0x06;
		
		EditText textBox = (EditText) findViewById(editTextId);
		if (textBox != null)
		{
			String value = textBox.getText().toString();
			if (isHexInput)
			{
				Integer eventVal = Integer.parseInt(value, 16);
				rv = eventVal;
			}
			else
			{							
				Integer eventVal = Integer.parseInt(value);
				rv = eventVal;
			}			
		}
		
		return rv;
	}
}
