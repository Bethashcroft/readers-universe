import { useEffect, useRef, useState } from "react";
import "./AvatarCropModal.css";

const VIEWPORT = 280;
const OUTPUT = 512;

interface AvatarCropModalProps {
  file: File;
  saving: boolean;
  onCancel: () => void;
  onSave: (cropped: File) => void;
}

function AvatarCropModal({
  file,
  saving,
  onCancel,
  onSave,
}: AvatarCropModalProps) {
  const [imageUrl, setImageUrl] = useState("");
  const [size, setSize] = useState<{ width: number; height: number } | null>(
    null,
  );
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const imgRef = useRef<HTMLImageElement>(null);
  const dragStart = useRef<{
    pointerX: number;
    pointerY: number;
    offsetX: number;
    offsetY: number;
  } | null>(null);

  useEffect(() => {
    const url = URL.createObjectURL(file);
    setImageUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);

  const baseScale = size ? VIEWPORT / Math.min(size.width, size.height) : 1;
  const scale = baseScale * zoom;

  const clampOffset = (x: number, y: number, currentScale: number) => {
    if (!size) return { x: 0, y: 0 };
    const maxX = Math.max(0, (size.width * currentScale - VIEWPORT) / 2);
    const maxY = Math.max(0, (size.height * currentScale - VIEWPORT) / 2);
    return {
      x: Math.min(maxX, Math.max(-maxX, x)),
      y: Math.min(maxY, Math.max(-maxY, y)),
    };
  };

  const handlePointerDown = (e: React.PointerEvent<HTMLDivElement>) => {
    e.currentTarget.setPointerCapture(e.pointerId);
    dragStart.current = {
      pointerX: e.clientX,
      pointerY: e.clientY,
      offsetX: offset.x,
      offsetY: offset.y,
    };
  };

  const handlePointerMove = (e: React.PointerEvent<HTMLDivElement>) => {
    if (!dragStart.current) return;
    const dx = e.clientX - dragStart.current.pointerX;
    const dy = e.clientY - dragStart.current.pointerY;
    setOffset(
      clampOffset(
        dragStart.current.offsetX + dx,
        dragStart.current.offsetY + dy,
        scale,
      ),
    );
  };

  const handlePointerUp = () => {
    dragStart.current = null;
  };

  const handleZoom = (value: number) => {
    setZoom(value);
    setOffset((prev) => clampOffset(prev.x, prev.y, baseScale * value));
  };

  const handleSave = () => {
    const img = imgRef.current;
    if (!img || !size) return;

    const canvas = document.createElement("canvas");
    canvas.width = OUTPUT;
    canvas.height = OUTPUT;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const sourceSize = VIEWPORT / scale;
    const sourceX = size.width / 2 - offset.x / scale - sourceSize / 2;
    const sourceY = size.height / 2 - offset.y / scale - sourceSize / 2;

    ctx.fillStyle = "#fff";
    ctx.fillRect(0, 0, OUTPUT, OUTPUT);
    ctx.drawImage(
      img,
      sourceX,
      sourceY,
      sourceSize,
      sourceSize,
      0,
      0,
      OUTPUT,
      OUTPUT,
    );

    canvas.toBlob(
      (blob) => {
        if (!blob) return;
        onSave(new File([blob], "avatar.jpg", { type: "image/jpeg" }));
      },
      "image/jpeg",
      0.9,
    );
  };

  return (
    <div className="crop-backdrop">
      <div className="crop-modal">
        <h2>Position your photo</h2>
        <p className="crop-hint">Drag to choose what shows inside the frame</p>
        <div
          className="crop-viewport"
          onPointerDown={handlePointerDown}
          onPointerMove={handlePointerMove}
          onPointerUp={handlePointerUp}
        >
          <img
            ref={imgRef}
            src={imageUrl}
            alt=""
            draggable={false}
            onLoad={(e) =>
              setSize({
                width: e.currentTarget.naturalWidth,
                height: e.currentTarget.naturalHeight,
              })
            }
            style={{
              transform: `translate(calc(-50% + ${offset.x}px), calc(-50% + ${offset.y}px))`,
              width: size ? size.width * scale : undefined,
            }}
          />
        </div>
        <label className="crop-zoom">
          Zoom
          <input
            type="range"
            min={1}
            max={3}
            step={0.01}
            value={zoom}
            onChange={(e) => handleZoom(Number(e.target.value))}
          />
        </label>
        <div className="crop-actions">
          <button
            className="btn btn-primary"
            onClick={handleSave}
            disabled={!size || saving}
          >
            {saving ? "Uploading..." : "Save Photo"}
          </button>
          <button
            className="btn btn-secondary"
            onClick={onCancel}
            disabled={saving}
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}

export default AvatarCropModal;
