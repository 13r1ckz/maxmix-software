//********************************************************
// PROJECT: MAXMIX
// AUTHOR: Ruben Henares
// EMAIL: rhenares0@gmail.com
//
// DECRIPTION:
//********************************************************

//---------------------------------------------------------
//---------------------------------------------------------
void UpdateLighting()
{
  if(stateDisplay == STATE_DISPLAY_SLEEP)
  {
    LightingBlackOut();
  }
  else if(sessionCount == 0)
  {
    LightingCircularFunk();
  }
  else if(mode == MODE_OUTPUT)
  {
    LightingVolume(&devices[itemIndexOutput], &settings.volumeColor1, &settings.volumeColor2);
  }
  else if(mode == MODE_APPLICATION)
  {
    LightingVolume(&sessions[itemIndexApp], &settings.volumeColor1, &settings.volumeColor2);
  }
  else if(mode == MODE_GAME)
  {
    LightingVolume(&sessions[itemIndexGameA], &settings.gameVolumeColor1, &settings.gameVolumeColor2);
  }

  // Push the colors to the pixels strip
  pixels->show();
}

//---------------------------------------------------------
void LightingBlackOut()
{
  // All black
  pixels->clear();
}

//---------------------------------------------------------
void LightingCircularFunk()
{
  uint32_t t = millis();
  uint16_t hue = t * 20;
  uint32_t rgbColor = pixels->ColorHSV(hue);
  uint16_t period = 500;

  uint8_t startOffset = 0;
  if ((t % period) > (period / 2))
  {
    startOffset = 1;
  }

  pixels->clear();
  pixels->setPixelColor(startOffset, rgbColor);
  pixels->setPixelColor(startOffset+2, rgbColor);
  pixels->setPixelColor(startOffset+4, rgbColor);
  pixels->setPixelColor(startOffset+6, rgbColor);
}

//---------------------------------------------------------
void LightingVolume(Item * item, Color * c1, Color * c2)
{
  // Dual colors circular lighting representing the volume.
  uint32_t volAcc = ((uint32_t)item->volume * 255 * PIXELS_COUNT) / 100;
  for (int i=0; i<PIXELS_COUNT; i++)
  {
    uint32_t amp = min(volAcc, 255);
    volAcc -= amp;

    // Linear interpolation to get the final color of each pixel.
    Color c = LerpColor(c1, c2, amp);
    pixels->setPixelColor(i, c.r, c.g, c.b);
  }
}

//---------------------------------------------------------
Color LerpColor(Color * c1, Color * c2, uint8_t fade)
{
  // Boundary cases don't work with bitwise stuff below.
  if (fade == 0)
  {
    return *c1;
  }
  else if (fade == 255)
  {
    return *c2;
  }

  uint16_t invFadeMod = (255 - fade) + 1;
  uint16_t fadeMod = fade + 1;

  Color cA = {
    (uint8_t)((uint16_t(c1->r) * invFadeMod) >> 8),
    (uint8_t)((uint16_t(c1->g) * invFadeMod) >> 8),
    (uint8_t)((uint16_t(c1->b) * invFadeMod) >> 8)
  };

  Color cB = {
    (uint8_t)((uint16_t(c2->r >> 16) * fadeMod) >> 8),
    (uint8_t)((uint16_t(c2->g >> 8) * fadeMod) >> 8),
    (uint8_t)((uint16_t(c2->b >> 0) * fadeMod) >> 8)
  };

  return {(c1->r + c2->r), (c1->g + c1->g), (c1->b + c2->b)};
}
