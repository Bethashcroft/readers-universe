import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import BarcodeScanner from "./BarcodeScanner";

type Reading = { getText: () => string } | undefined;
type OnReading = (
  result: Reading,
  error: unknown,
  controls: { stop: () => void },
) => void;

const { mockDecode, mockStop } = vi.hoisted(() => ({
  mockDecode: vi.fn(),
  mockStop: vi.fn(),
}));

vi.mock("@zxing/browser", () => ({
  BrowserMultiFormatReader: class {
    decodeFromConstraints = mockDecode;
  },
}));

vi.mock("@zxing/library", () => ({
  BarcodeFormat: { EAN_13: 7 },
  DecodeHintType: { POSSIBLE_FORMATS: 2 },
}));

const reads = (code: string) => ({ getText: () => code });

describe("BarcodeScanner", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("hands back the first ISBN it reads and turns the camera off", async () => {
    let onReading: OnReading = () => {};
    mockDecode.mockImplementation(async (_constraints, _video, callback) => {
      onReading = callback;
      return { stop: mockStop };
    });
    const onScan = vi.fn();
    render(<BarcodeScanner onScan={onScan} onClose={vi.fn()} />);

    await waitFor(() => expect(mockDecode).toHaveBeenCalled());

    act(() => {
      onReading(reads("5012345678900"), undefined, { stop: mockStop });
      onReading(undefined, new Error("nothing yet"), { stop: mockStop });
      onReading(reads("9780261103344"), undefined, { stop: mockStop });
      onReading(reads("9780261103344"), undefined, { stop: mockStop });
    });

    expect(onScan).toHaveBeenCalledTimes(1);
    expect(onScan).toHaveBeenCalledWith("9780261103344");
    expect(mockStop).toHaveBeenCalled();
  });

  it("explains when the browser blocks the camera", async () => {
    mockDecode.mockRejectedValue(new DOMException("denied", "NotAllowedError"));
    render(<BarcodeScanner onScan={vi.fn()} onClose={vi.fn()} />);

    expect(
      await screen.findByText(/Your browser blocked the camera/),
    ).toBeInTheDocument();
  });

  it("turns the camera off when you cancel", async () => {
    mockDecode.mockResolvedValue({ stop: mockStop });
    const onClose = vi.fn();
    const { unmount } = render(
      <BarcodeScanner onScan={vi.fn()} onClose={onClose} />,
    );
    await waitFor(() => expect(mockDecode).toHaveBeenCalled());

    await userEvent.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onClose).toHaveBeenCalled();

    unmount();
    expect(mockStop).toHaveBeenCalled();
  });
});
