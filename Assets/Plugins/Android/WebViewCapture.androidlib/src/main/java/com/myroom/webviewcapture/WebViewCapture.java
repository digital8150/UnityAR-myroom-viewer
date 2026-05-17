package com.myroom.webviewcapture;

import android.app.Activity;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.util.Log;
import android.webkit.WebView;

import java.io.ByteArrayOutputStream;
import java.lang.reflect.Field;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;

public class WebViewCapture {
    private static final String TAG = "WebViewCapture";

    /**
     * Synchronously captures the contents of the gree WebViewObject's underlying
     * android.webkit.WebView into a JPEG byte[]. Must be invoked from Unity's
     * main thread. Internally hops to the Android UI thread to perform draw().
     *
     * @param activity      Current Unity activity.
     * @param pluginObject  The net.gree.unitywebview.CWebViewPlugin instance held
     *                      privately by WebViewObject (`webView` field).
     * @param quality       JPEG quality (0..100).
     * @return JPEG bytes, or null on failure.
     */
    public static byte[] capture(final Activity activity, final Object pluginObject, final int quality) {
        if (activity == null || pluginObject == null) return null;

        final byte[][] result = new byte[1][];
        final CountDownLatch latch = new CountDownLatch(1);

        activity.runOnUiThread(new Runnable() {
            @Override public void run() {
                try {
                    WebView wv = findWebView(pluginObject);
                    if (wv == null) {
                        Log.e(TAG, "Underlying WebView not found on plugin instance");
                        return;
                    }
                    int w = wv.getWidth();
                    int h = wv.getHeight();
                    if (w <= 0 || h <= 0) {
                        Log.e(TAG, "WebView has zero size (" + w + "x" + h + ")");
                        return;
                    }
                    Bitmap bmp = Bitmap.createBitmap(w, h, Bitmap.Config.ARGB_8888);
                    Canvas canvas = new Canvas(bmp);
                    wv.draw(canvas);
                    ByteArrayOutputStream bos = new ByteArrayOutputStream();
                    bmp.compress(Bitmap.CompressFormat.JPEG, quality, bos);
                    bmp.recycle();
                    result[0] = bos.toByteArray();
                } catch (Throwable t) {
                    Log.e(TAG, "capture failed", t);
                } finally {
                    latch.countDown();
                }
            }
        });

        try {
            if (!latch.await(5, TimeUnit.SECONDS)) {
                Log.e(TAG, "capture timed out");
            }
        } catch (InterruptedException ignored) {
            Thread.currentThread().interrupt();
        }
        return result[0];
    }

    private static WebView findWebView(Object plugin) {
        Class<?> cls = plugin.getClass();
        while (cls != null) {
            for (Field f : cls.getDeclaredFields()) {
                if (WebView.class.isAssignableFrom(f.getType())) {
                    try {
                        f.setAccessible(true);
                        Object v = f.get(plugin);
                        if (v instanceof WebView) return (WebView) v;
                    } catch (IllegalAccessException ignored) { }
                }
            }
            cls = cls.getSuperclass();
        }
        return null;
    }
}
