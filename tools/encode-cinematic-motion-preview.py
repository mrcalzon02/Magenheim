#!/usr/bin/env python3
"""Encode Blender-rendered cinematic motion-proof frames into a silent MP4."""
from __future__ import annotations
import argparse
from pathlib import Path
from PIL import Image
import imageio_ffmpeg

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--frames",type=Path,required=True)
    ap.add_argument("--output",type=Path,required=True)
    ap.add_argument("--fps",type=float,default=12.0)
    ns=ap.parse_args()
    frames=sorted(ns.frames.glob("*.png"))
    if len(frames)<300:
        raise SystemExit(f"Expected at least 300 motion-proof frames; got {len(frames)}")
    first=Image.open(frames[0]).convert("RGB")
    size=first.size
    ns.output.parent.mkdir(parents=True,exist_ok=True)
    writer=imageio_ffmpeg.write_frames(
        str(ns.output),size,fps=ns.fps,codec="libx264",
        pix_fmt_in="rgb24",pix_fmt_out="yuv420p",
        output_params=["-movflags","+faststart","-crf","23","-an"],
    )
    writer.send(None)
    try:
        writer.send(first.tobytes())
        for path in frames[1:]:
            with Image.open(path) as im:
                rgb=im.convert("RGB")
                if rgb.size!=size:
                    raise RuntimeError(f"Frame size changed: {path} {rgb.size} != {size}")
                writer.send(rgb.tobytes())
    finally:
        writer.close()
    if not ns.output.is_file() or ns.output.stat().st_size<100000:
        raise SystemExit(f"Motion proof output missing or suspiciously small: {ns.output}")
    print(f"ENCODED silent motion proof: {len(frames)} frames @ {ns.fps:g} fps -> {ns.output} ({ns.output.stat().st_size} bytes)")

if __name__=="__main__":
    main()
