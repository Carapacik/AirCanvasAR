package com.carapacik.aircanvasar;

import android.content.ContentResolver;
import android.content.ContentValues;
import android.content.Context;
import android.net.Uri;
import android.provider.MediaStore;
import java.io.OutputStream;
import java.io.IOException;

public final class GalleryWriter {
    private GalleryWriter() {}
    public static String save(Context context, byte[] png, String name) throws IOException {
        ContentResolver resolver = context.getContentResolver();
        ContentValues values = new ContentValues();
        values.put(MediaStore.Images.Media.DISPLAY_NAME, name);
        values.put(MediaStore.Images.Media.MIME_TYPE, "image/png");
        values.put(MediaStore.Images.Media.RELATIVE_PATH, "Pictures/AirCanvasAR");
        values.put(MediaStore.Images.Media.IS_PENDING, 1);
        Uri uri = resolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
        if (uri == null) throw new IOException("Could not create a gallery image.");
        try {
            try (OutputStream output = resolver.openOutputStream(uri)) {
                if (output == null) throw new IOException("Could not open the gallery image.");
                output.write(png);
            }
            values.clear();
            values.put(MediaStore.Images.Media.IS_PENDING, 0);
            resolver.update(uri, values, null, null);
            return uri.toString();
        } catch (Exception failure) {
            resolver.delete(uri, null, null);
            throw new IOException("Could not save the gallery image.", failure);
        }
    }
}
