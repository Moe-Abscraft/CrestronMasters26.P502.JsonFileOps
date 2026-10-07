/**
 * Every icon the UI uses, in one place. Font Awesome Pro renders these as
 * inline SVG, and only the icons named here end up in the bundle -- there is
 * no icon font to download, so nothing depends on the path the panel or
 * VC-4 serves the app from.
 *
 * Solid replaces Remix "-fill" icons, regular replaces "-line" icons.
 * Use in a template as: <fa-icon [icon]="icons.close"></fa-icon>
 */
import {
  faCircleExclamation,
  faMicrophone,
  faMicrophoneSlash,
  faStar,
  faVolume,
  faVolumeXmark,
} from '@fortawesome/pro-solid-svg-icons';
import { faCheck, faXmark } from '@fortawesome/pro-regular-svg-icons';

export const Icons = {
  volume: faVolume,
  volumeMuted: faVolumeXmark,
  mic: faMicrophone,
  micMuted: faMicrophoneSlash,
  preset: faStar,
  check: faCheck,
  close: faXmark,
  warning: faCircleExclamation,
} as const;
