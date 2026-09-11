import { API_ORIGIN } from "../api/client";
import "./Avatar.css";

type AvatarProps = {
  url: string;
  name: string;
  size?: number;
};

function Avatar({ url, name, size = 44 }: AvatarProps) {
  return (
    <span
      className="avatar"
      style={{ width: size, height: size, fontSize: Math.round(size * 0.42) }}
    >
      {url ? <img src={`${API_ORIGIN}${url}`} alt="" /> : name.charAt(0)}
    </span>
  );
}

export default Avatar;
