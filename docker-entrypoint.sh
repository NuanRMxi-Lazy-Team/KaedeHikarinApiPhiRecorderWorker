#!/bin/sh
set -e

# Linux headless rendering uses an EGL desktop OpenGL pbuffer and does not
# require DISPLAY, X11, or Xvfb. GPU visibility is provided by the container
# runtime, for example --gpus all or --device /dev/dri/renderD128.
exec dotnet KaedeHikarinCialloTeam.PhiRecorder.Worker.dll
