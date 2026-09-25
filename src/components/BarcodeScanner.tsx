import { useEffect, useEffectEvent, useRef, useState } from "react";
import Modal from "./Modal";
import "./BarcodeScanner.css";

const isIsbn = (code: string) => /^97[89]\d{10}$/.test(code);

function cameraProblem(err: unknown) {
  const name = err instanceof DOMException ? err.name : "";

  if (name === "NotAllowedError") {
    return "Your browser blocked the camera. Allow it for this site in your browser settings, then try again.";
  }

  if (name === "NotFoundError" || name === "OverconstrainedError") {
    return "We couldn't find a camera on this device.";
  }

  if (name === "NotReadableError") {
    return "Your camera is being used by another app. Close it there and try again.";
  }

  return "The camera didn't start. Try again, or type the ISBN instead.";
}

type BarcodeScannerProps = {
  onScan: (isbn: string) => void;
  onClose: () => void;
};

function BarcodeScanner({ onScan, onClose }: BarcodeScannerProps) {
  const video = useRef<HTMLVideoElement>(null);
  const [problem, setProblem] = useState("");
  const handleScan = useEffectEvent(onScan);

  useEffect(() => {
    let active = true;
    let stop = () => {};

    const start = async () => {
      try {
        const [{ BrowserMultiFormatReader }, { BarcodeFormat, DecodeHintType }] =
          await Promise.all([import("@zxing/browser"), import("@zxing/library")]);
        if (!active || !video.current) return;

        const reader = new BrowserMultiFormatReader(
          new Map([[DecodeHintType.POSSIBLE_FORMATS, [BarcodeFormat.EAN_13]]]),
        );

        const controls = await reader.decodeFromConstraints(
          { video: { facingMode: "environment" } },
          video.current,
          (result, _error, scanning) => {
            const code = result?.getText();
            if (!active || !code || !isIsbn(code)) return;

            active = false;
            scanning.stop();
            handleScan(code);
          },
        );

        stop = () => controls.stop();
        if (!active) stop();
      } catch (err) {
        console.error("Camera failed:", err);
        if (active) setProblem(cameraProblem(err));
      }
    };

    start();

    return () => {
      active = false;
      stop();
    };
  }, []);

  return (
    <Modal title="Scan the barcode" onClose={onClose} className="barcode-modal">
      {problem ? (
        <p className="form-error">{problem}</p>
      ) : (
        <>
          <div className="barcode-frame">
            <video ref={video} className="barcode-video" muted playsInline />
            <span className="barcode-guide" aria-hidden="true" />
          </div>
          <p className="barcode-hint">
            Hold the barcode on the back of the book inside the box.
          </p>
        </>
      )}

      <button type="button" className="btn btn-secondary" onClick={onClose}>
        Cancel
      </button>
    </Modal>
  );
}

export default BarcodeScanner;
