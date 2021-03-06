/* Android activity for Gforth on Android

  Copyright (C) 2013 Free Software Foundation, Inc.

  This file is part of Gforth.

  Gforth is free software; you can redistribute it and/or
  modify it under the terms of the GNU General Public License
  as published by the Free Software Foundation, either version 3
  of the License, or (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, see http://www.gnu.org/licenses/.
*/

package gnu.gforth;

import android.os.Bundle;
import android.os.Handler;
import android.text.ClipboardManager;
import android.content.pm.ActivityInfo;
import android.content.pm.PackageManager;
import android.content.res.Configuration;
import android.media.MediaPlayer;
import android.media.MediaPlayer.OnVideoSizeChangedListener;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.content.Context;
import android.view.View;
import android.view.Window;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.Surface;
import android.view.SurfaceHolder;
import android.view.OrientationEventListener;
import android.view.ViewTreeObserver.OnGlobalLayoutListener;
import android.view.WindowManager;
import android.view.inputmethod.InputConnection;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.BaseInputConnection;
import android.view.inputmethod.InputMethodManager;
import android.view.inputmethod.CompletionInfo;
import android.text.InputType;
import android.text.SpannableStringBuilder;
import android.text.Editable;
import android.app.Activity;
import android.app.ProgressDialog;
import android.util.Log;
import java.lang.Object;
import java.lang.Runnable;
import java.lang.String;
import java.io.File;

public class Gforth
    extends android.app.Activity
    implements KeyEvent.Callback,
	       OnVideoSizeChangedListener,
	       LocationListener,
	       SensorEventListener,
	       SurfaceHolder.Callback2,
	       OnGlobalLayoutListener /* ,
	       ClipboardManager.OnPrimaryClipChangedListener */ {
    private long argj0=1000; // update every second
    private double argf0=10;    // update every 10 meters
    private String args0="gps";
    private Sensor argsensor;
    private Gforth gforth;
    private LocationManager locationManager;
    private SensorManager sensorManager;
    private ClipboardManager clipboardManager;
    private boolean started=false;
    private boolean libloaded=false;

    public Handler handler;
    public Runnable startgps;
    public Runnable stopgps;
    public Runnable startsensor;
    public Runnable stopsensor;
    public Runnable showprog;
    public Runnable hideprog;
    public ProgressDialog progress;

    private static final String META_DATA_LIB_NAME = "android.app.lib_name";
    private static final String TAG = "Gforth";

    public native void onEventNative(int type, Object event);
    public native void onEventNative(int type, int event);
    public native void callForth(int xt); // !! use long for 64 bits !!
    public native void startForth();

    // own subclasses
    public class RunForth implements Runnable {
	int xt;
	RunForth(int initxt) {
	    xt = initxt;
	}
	public void run() {
	    callForth(xt);
	}
    }

    static class MyInputConnection extends BaseInputConnection {
	private SpannableStringBuilder mEditable;
	private ContentView mView;
	
	public MyInputConnection(View targetView, boolean fullEditor) {
	    super(targetView, fullEditor);
	    mView = (ContentView) targetView;
	}
	
	public Editable getEditable() {
	    if (mEditable == null) {
		mEditable = (SpannableStringBuilder) Editable.Factory.getInstance()
		    .newEditable("Gforth Terminal");
	    }
	    return mEditable;
	}

	public boolean commitText(CharSequence text, int newcp) {
	    if(text != null) {
		mView.mActivity.onEventNative(12, text.toString());
	    } else {
		mView.mActivity.onEventNative(12, 0);
	    }
	    return true;
	}
	public boolean setComposingText(CharSequence text, int newcp) {
	    if(text != null) {
		mView.mActivity.onEventNative(13, text.toString());
	    } else {
		mView.mActivity.onEventNative(13, "");
	    }
	    return true;
	}
	public boolean finishComposingText () {
	    mView.mActivity.onEventNative(12, 0);
	    return true;
	}
	public boolean commitCompletion(CompletionInfo text) {
	    if(text != null) {
		mView.mActivity.onEventNative(12, text.toString());
	    } else {
		mView.mActivity.onEventNative(12, 0);
	    }
	    return true;
	}
	public boolean deleteSurroundingText (int before, int after) {
	    mView.mActivity.onEventNative(11, "deleteSurroundingText");
	    mView.mActivity.onEventNative(10, before);
	    mView.mActivity.onEventNative(10, after);
	    return true;
	}
	public boolean setComposingRegion (int start, int end) {
	    mView.mActivity.onEventNative(11, "setComposingRegion");
	    mView.mActivity.onEventNative(10, start);
	    mView.mActivity.onEventNative(10, end);
	    return true;
	}
	public boolean sendKeyEvent (KeyEvent event) {
	    mView.mActivity.onEventNative(0, event);
	    return true;
	}
    }

    static class ContentView extends View {
        Gforth mActivity;
	InputMethodManager mManager;
	EditorInfo moutAttrs;
	MyInputConnection mInputConnection;

        public ContentView(Gforth context) {
            super(context);
	    mActivity=context;
	    mManager = (InputMethodManager)context.getSystemService(Context.INPUT_METHOD_SERVICE);
	    setFocusable(true);
	    setFocusableInTouchMode(true);
        }
	public void showIME() {
	    mManager.showSoftInput(this, 0);
	}
	public void hideIME() {
	    mManager.hideSoftInputFromWindow(getWindowToken(), 0);
	}

	@Override
	public boolean onCheckIsTextEditor () {
	    return true;
	}
	@Override
	public InputConnection onCreateInputConnection (EditorInfo outAttrs) {
	    moutAttrs=outAttrs;
	    outAttrs.inputType = InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_AUTO_COMPLETE | InputType.TYPE_TEXT_FLAG_AUTO_CORRECT;
	    outAttrs.initialSelStart = 1;
	    outAttrs.initialSelEnd = 1;
	    outAttrs.packageName = "gnu.gforth";
	    mInputConnection = new MyInputConnection(this, true);
	    return mInputConnection;
	}
	@Override
	public void onSizeChanged(int w, int h, int oldw, int oldh) {
	    mActivity.onEventNative(14, w);
	    mActivity.onEventNative(15, h);
	}
	@Override
	public boolean dispatchKeyEvent (KeyEvent event) {
	    mActivity.onEventNative(0, event);
	    return true;
	}
	@Override
	public boolean onKeyDown (int keyCode, KeyEvent event) {
	    mActivity.onEventNative(0, event);
	    return true;
	}
	@Override
	public boolean onKeyUp (int keyCode, KeyEvent event) {
	    mActivity.onEventNative(0, event);
	    return true;
	}
	@Override
	public boolean onKeyMultiple (int keyCode, int repeatCount, KeyEvent event) {
	    mActivity.onEventNative(0, event);
	    return true;
	}
	@Override
	public boolean onKeyLongPress (int keyCode, KeyEvent event) {
	    mActivity.onEventNative(0, event);
	    return true;
	}
	/* @Override
	public boolean onKeyPreIme (int keyCode, KeyEvent event) {
	    mActivity.onEventNative(0, event);
	    return true;
	    } */
    }
    ContentView mContentView;

    public void hideProgress() {
	if(progress!=null) {
	    progress.dismiss();
	    progress=null;
	}
    }
    public void showProgress() {
	progress = ProgressDialog.show(this, "Unpacking files",
				       "please wait", true);
    }
    public void doneProgress() {
	progress.setMessage("Done; restart Gforth");
    }

    public void showIME() {
	mContentView.showIME();
    }
    public void hideIME() {
	mContentView.hideIME();
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        ActivityInfo ai;
        String libname = "gforth";

	gforth=this;
	progress=null;

        getWindow().takeSurface(this);
        // getWindow().setFormat(PixelFormat.RGB_565);
        getWindow().setSoftInputMode(
                WindowManager.LayoutParams.SOFT_INPUT_STATE_UNSPECIFIED
                | WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE);


        mContentView = new ContentView(this);
        setContentView(mContentView);
        mContentView.requestFocus();
        mContentView.getViewTreeObserver().addOnGlobalLayoutListener(this);
	// setRetainInstance(true);

	try {
            ai = getPackageManager().getActivityInfo(getIntent().getComponent(), PackageManager.GET_META_DATA);
            if (ai.metaData != null) {
                String ln = ai.metaData.getString(META_DATA_LIB_NAME);
                if (ln != null) libname = ln;
            }
        } catch (PackageManager.NameNotFoundException e) {
            throw new RuntimeException("Error getting activity info", e);
        }
	if(!libloaded) {
	    Log.v(TAG, "open library: " + libname);
	    System.loadLibrary(libname);
	    libloaded=true;
	} else {
	    Log.v(TAG, "Library already loaded");
	}
	super.onCreate(savedInstanceState);
    }

    @Override protected void onStart() {
	super.onStart();
	if(!started) {
	    locationManager=(LocationManager)getSystemService(Context.LOCATION_SERVICE);
	    sensorManager=(SensorManager)getSystemService(Context.SENSOR_SERVICE);
	    clipboardManager=(ClipboardManager)getSystemService(Context.CLIPBOARD_SERVICE);
	    handler=new Handler();
	    startgps=new Runnable() {
		    public void run() {
			locationManager.requestLocationUpdates(args0, argj0, (float)argf0, (LocationListener)gforth);
		    }
		};
	    stopgps=new Runnable() {
		    public void run() {
		    locationManager.removeUpdates((LocationListener)gforth);
		    }
		};
	    startsensor=new Runnable() {
		    public void run() {
			sensorManager.registerListener((SensorEventListener)gforth, argsensor, (int)argj0);
		    }
		};
	    stopsensor=new Runnable() {
		    public void run() {
			sensorManager.unregisterListener((SensorEventListener)gforth, argsensor);
		    }
		};
	    showprog=new Runnable() {
		    public void run() {
			showProgress();
		    }
		};
	    hideprog=new Runnable() {
		    public void run() {
			doneProgress();
		    }
		};
	    startForth();
	    started=true;
	}
    }
   
    @Override
    public boolean dispatchKeyEvent (KeyEvent event) {
	onEventNative(0, event);
	return true;
    }
    @Override
    public boolean onKeyDown (int keyCode, KeyEvent event) {
	onEventNative(0, event);
	return true;
    }
    @Override
    public boolean onKeyUp (int keyCode, KeyEvent event) {
	onEventNative(0, event);
	return true;
    }
    @Override
    public boolean onKeyMultiple (int keyCode, int repeatCount, KeyEvent event) {
	onEventNative(0, event);
	return true;
    }
    @Override
    public boolean onKeyLongPress (int keyCode, KeyEvent event) {
	onEventNative(0, event);
	return true;
    }
    @Override
    public boolean onTouchEvent(MotionEvent event) {
	onEventNative(1, event);
	return true;
    }

    // location requests
    public void onLocationChanged(Location location) {
	// Called when a new location is found by the network location provider.
	onEventNative(2, location);
    }
    public void onStatusChanged(String provider, int status, Bundle extras) {}
    public void onProviderEnabled(String provider) {}
    public void onProviderDisabled(String provider) {}

    // sensor events
    public void onAccuracyChanged(Sensor sensor, int accuracy) {}
    public void onSensorChanged(SensorEvent event) {
	onEventNative(3, event);
    }

    // surface stuff
    public void surfaceCreated(SurfaceHolder holder) {
	onEventNative(4, holder.getSurface());
    }
    
    public class surfacech {
	Surface surface;
	int format;
	int width;
	int height;

	surfacech(Surface s, int f, int w, int h) {
	    surface=s;
	    format=f;
	    width=w;
	    height=h;
	}
    }

    public void surfaceChanged(SurfaceHolder holder, int format, int width, int height) {
	surfacech sch = new surfacech(holder.getSurface(), format, width, height);

	onEventNative(5, sch);
    }
    
    public void surfaceRedrawNeeded(SurfaceHolder holder) {
	onEventNative(6, holder.getSurface());
    }

    public void surfaceDestroyed(SurfaceHolder holder) {
	onEventNative(7, holder.getSurface());
    }

    // global layout
    public void onGlobalLayout() {
	onEventNative(8, 0);
    }

    // media player
    public class mpch {
	MediaPlayer mediaPlayer;
	int format;
	int width;
	int height;

	mpch(MediaPlayer m, int w, int h) {
	    mediaPlayer=m;
	    width=w;
	    height=h;
	}
    }

    @Override
    public void onVideoSizeChanged(MediaPlayer mp, int width, int height) {
	mpch newmp = new mpch(mp, width, height);

	onEventNative(9, newmp);
    }
    /*
    @Override
    public void onPrimaryClipChanged() {
	onEventNative(16, 0);
    }
    */
    @Override
    public void onConfigurationChanged(Configuration newConfig) {
	Log.v(TAG, "Configuration changed");
	super.onConfigurationChanged(newConfig);
	
	onEventNative(17, newConfig.orientation);
    }
}
