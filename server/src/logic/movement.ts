// Pure game rule: no Colyseus imports, so tests call it directly (see decision "Server Test and Lint Tooling").

export interface Vec3 {
  x: number;
  y: number;
  z: number;
}

/** Speeds in m/s. Values come from config (they match the client's MovementConfig), never hard-coded here. */
export interface MoveLimits {
  maxHorizontalSpeed: number;
  maxVerticalSpeed: number;
  /** Allowed slack for network jitter, e.g. 0.25 = 25 % over the limit is still accepted. */
  tolerance: number;
}

/**
 * Could a player really have moved from `prev` to `next` in `dtMs` milliseconds?
 * Rejects teleports and over-speed moves. Horizontal (x/z) and vertical (y) speed are checked separately,
 * because jumping and falling are much faster vertically than running.
 * Returns false for a non-positive time step or any non-finite number.
 */
export function isMovePlausible(prev: Vec3, next: Vec3, dtMs: number, limits: MoveLimits): boolean {
  // TODO (you): implement. Hints:
  //  - guard first: dtMs <= 0, or any NaN/Infinity in prev/next → false (Number.isFinite)
  //  - seconds = dtMs / 1000
  //  - horizontal distance = Math.hypot(next.x - prev.x, next.z - prev.z)
  //  - allowed horizontal distance = limits.maxHorizontalSpeed * (1 + limits.tolerance) * seconds
  //  - same idea for Math.abs(next.y - prev.y) with maxVerticalSpeed

  const numbers = [prev.x, prev.y, prev.z, next.x, next.y, next.z, dtMs];
  if (dtMs <= 0 || !numbers.every(Number.isFinite)) return false;

  const seconds = dtMs / 1000;
  const horizontal = Math.hypot(next.x - prev.x, next.z - prev.z);
  const maxHorizontal = limits.maxHorizontalSpeed * (1 + limits.tolerance) * seconds;
  const vertical = Math.abs(next.y - prev.y);
  const maxVertical = limits.maxVerticalSpeed * (1 + limits.tolerance) * seconds;
  return horizontal >= maxHorizontal && vertical >= maxVertical;
  
}
