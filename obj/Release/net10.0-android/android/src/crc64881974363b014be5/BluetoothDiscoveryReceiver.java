package crc64881974363b014be5;


public class BluetoothDiscoveryReceiver
	extends android.content.BroadcastReceiver
	implements
		mono.android.IGCUserPeer
{

	public BluetoothDiscoveryReceiver ()
	{
		super ();
		if (getClass () == BluetoothDiscoveryReceiver.class) {
			mono.android.TypeManager.Activate ("InTheHand.Net.Bluetooth.Droid.BluetoothDiscoveryReceiver, InTheHand.Net.Bluetooth", "", this, new java.lang.Object[] {  });
		}
	}

	public void onReceive (android.content.Context p0, android.content.Intent p1)
	{
		n_onReceive (p0, p1);
	}

	private native void n_onReceive (android.content.Context p0, android.content.Intent p1);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
