# 360° Spherical Panoramic Photography & Processing Workflow

**Camera System:** Sony Alpha ILCE-6600 (APS-C) + Sony E 16-55mm f/2.8 G  
**Mounting:** Panoramic Gimbal Head (Portrait orientation, $10^\circ$ Yaw / $7.5^\circ$ Pitch rasterization)  
**Selected Focal Length:** 28mm  
**Total Dataset:** 38 RAW (.ARW) frames  

---

## 1. Hardware & Physical Rig Setup

- [ ] **Orientation:** Mount the Sony A6600 in **Portrait (vertical)** orientation using an L-bracket or panoramic gimbal plate.
- [ ] **Focal Length:** Set the lens zoom to **28mm**. Secure the zoom ring with gaffer tape so it cannot shift during rotation.
- [ ] **No-Parallax Point (NPP):** Align the lens entrance pupil directly over the vertical rotation axis of your gimbal head to prevent parallax errors between near grass and distant objects.
- [ ] **Leveling:** Ensure the tripod base and gimbal head are perfectly level.

---

## 2. Camera Menu & Exposure Configuration

- [ ] **Shooting Mode:** **Manual (M)**
- [ ] **Aperture:** **f/11** (provides sharp detail across the frame and broad depth of field).
- [ ] **ISO:** **100** (Base ISO for maximum dynamic range and minimal noise).
- [ ] **Focus Mode:** **Manual Focus (MF)**
  - Set focus to **Hyperfocal Distance (~5.5 meters)** or **Infinity ($\infty$)**.
  - *Do NOT focus on close-up grass (~3m), or distant landscape will lose sharpness.*
- [ ] **White Balance:** **Daylight / Sunny (Fixed Preset ~5500K)**. *Do NOT use Auto White Balance (AWB).*
- [ ] **Format:** **RAW (.ARW)**
- [ ] **Triggering:** Sony Imaging Edge App (or 2-second Self-Timer) to avoid camera shake.

---

## 3. Metering for Outdoor High Contrast

1. **Target the Sky:** Point camera toward the sky near the sun (avoiding direct sun core).
2. **Enable Zebra Overlay:** Set `Menu → Zebra Setting → 100+`.
3. **Set Shutter Speed:** Increase shutter speed until Zebra stripes on the sky disappear (starting baseline around **1/500s**).
4. **Check Histogram:** Ensure shadow detail in the dark cornfield is not completely crushed against the left wall.
5. **Lock Exposure:** Keep this exact shutter speed locked across all 38 frames.

---

## 4. Safe Practices for Your Panorama

1. **Move Quickly Through the Sun Grid:** When you rotate the gimbal head to the position facing the sun, take the shot immediately and rotate to the next angle. Do not pause or leave the camera sitting on that position.
2. **Keep Lens Cap On During Setup:** Keep the lens covered while leveling the tripod, adjusting the gimbal, or taking breaks.
3. **Use the Lens Hood:** Always keep the lens hood attached to reduce internal lens reflections and severe lens flare artifacts in surrounding shots.
4. **Protect Your Eyes:** Never look directly at the sun with your bare eyes when manually checking grid positions on the field.

---

## 5. The 72-Shot Symmetrical Panning Grid

**Recommended Focal Length:** 28mm (Portrait orientation, native 3:2 aspect ratio).  
This 5-row layout uses a symmetrical $37.5^\circ$ vertical increment, perfectly matching your head's $7.5^\circ$ pitch detents (+9, +5, 0, -5, -9 clicks) and $10^\circ$ yaw detents (2 and 4 clicks).

### Grid Layout Table

| Row | Target Pitch | Pitch Detents ($7.5^\circ$ steps) | Yaw Step | Yaw Detents ($10^\circ$ steps) | Shots per Row |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1 (Upper Sky)** | **$+67.5^\circ$** | $+9$ clicks up | Every **$40^\circ$** | Every **4 clicks** | **9 shots** |
| **2 (Mid Sky)** | **$+37.5^\circ$** | $+5$ clicks up | Every **$20^\circ$** | Every **2 clicks** | **18 shots** |
| **3 (Horizon)** | **$0.0^\circ$** | 0 clicks (level) | Every **$20^\circ$** | Every **2 clicks** | **18 shots** |
| **4 (Ground)** | **$-37.5^\circ$** | $-5$ clicks down | Every **$20^\circ$** | Every **2 clicks** | **18 shots** |
| **5 (Lower Ground)**| **$-67.5^\circ$** | $-9$ clicks down | Every **$40^\circ$** | Every **4 clicks** | **9 shots** |
| **Total** | | | | | **72 shots** |

---

## 6. Rationale & Grid Layout Explanation

### Why $37.5^\circ$ Symmetry Works
* **Symmetrical Pitch Steps:** The pitch axis transitions in exact $37.5^\circ$ leaps from level ($0^\circ \rightarrow \pm 37.5^\circ \rightarrow \pm 67.5^\circ$). This eliminates confusion in the field — the upper and lower rows mirror each other exactly at $+5$ and $-5$ clicks ($+37.5^\circ / -37.5^\circ$) and $+9$ and $-9$ clicks ($+67.5^\circ / -67.5^\circ$).
* **Complete Spherical Coverage Without Cap Shots:** Tilting the outer rows to $\pm 67.5^\circ$ allows the top and bottom frame edges to reach beyond $+90^\circ$ and $-90^\circ$, sealing both the Zenith and Nadir completely without requiring standalone cap shots.
* **Overlaps:**
  * **Horizontal:** $20^\circ$ yaw steps on a $36^\circ$ HFOV (24mm) yield a generous **44% overlap** (and **35% overlap** at 28mm).
  * **Vertical:** $37.5^\circ$ pitch steps on a $52^\circ$ VFOV (24mm) yield a reliable **28% overlap** (and **17% overlap** at 28mm).

### Focal Length Flexibility (24mm to 28mm)
* You are not locked strictly to 24mm. Zooming in slightly up to **28mm** works seamlessly on this same grid.
* At 28mm, horizontal overlap drops slightly to ~35% and vertical overlap to ~17%, which is still well above Hugin’s minimum requirement (~15%) and produces a higher resolution master image.

---

## 7. Post-Processing & Unity Pipeline

### Phase 1: Batch RAW Pre-Processing (RawTherapee)
1. Launch RawTherapee and navigate to your dataset directory in the **File Browser** tab.
2. Verify that images render in their native **3:2 aspect ratio** ($6024 \times 4024$ px). *RawTherapee automatically bypasses in-camera 9:16 crop tags out of the box.*
3. Open any frame in the Editor view and apply the following baseline adjustments:
   - **White Balance (`Color` tab):** Set Method to `Daylight` or manually set Temperature to **5500K** (Color Temperature).
   - **Dynamic Range (`Exposure` tab):** Enable `Shadows/Highlights` — increase Shadows slider to lift ground detail and decrease Highlights slider to preserve sky detail.
   - **Lens Profile (`Transform` tab):** Under *Lens Geometry*, check `Profiled Lens Correction` and select **Auto-match correction** (uses Lensfun database for the Sony E 16-55mm f/2.8 G).
4. Copy processing profile (`Ctrl + C`), select all 38 images in the File Browser (`Ctrl + A`), and paste the settings (`Ctrl + V`) so every frame has identical exposure and geometry corrections.
5. Send all 38 images to the Queue (`B`), switch to the **Queue** tab, set output format to **TIFF (16-bit)** (Uncompressed or LZW), and click **Start Processing**.

### Phase 2: Stitching (Hugin)
1. **Import:** Load the 38 exported TIFF files into Hugin.
2. **Apply Template:** Load your pre-saved `.pto` template containing the hardcoded 38-shot angle geometry.
3. **Add settings in `.pto` file to avoid red areas**: #hugin_enblendOptions --preassemble --primary-seam-generator=nft --blend-colorspace=identity
4. **Output Render:** Export as an **Equirectangular Projection (2:1 aspect ratio, $8192 \times 4096$ px or $16384 \times 8192$ px)** as a 16-bit TIFF/PNG.


### Phase 3: Photoshop Retouching & Unity Import
1. Open stitched panorama in Photoshop; patch the Nadir (tripod footprint) at the bottom using `Filter → Distort → Polar Coordinates`.
2. Import final 8K equirectangular image into Unity:
   - **Texture Type:** `Default`
   - **Texture Shape:** `Cube`
   - **Mapping:** `Latitude Longitude (Layout)`
   - **Max Size:** `4096`
   - **Compression:** `High Quality` / `BC7`
3. Assign texture to a `Skybox/Cubemap` Material and apply to Environment Lighting.

