import type { RequestHandler } from 'express';

export interface HopFenceOptions {
  /**
   * Whether the diagnostic probe is enabled.
   * Can also be set via process.env.HOPFENCE_ENABLED === 'true'.
   * @default false
   */
  enabled?: boolean;

  /**
   * Bearer authentication token required to query the probe.
   * Can also be set via process.env.HOPFENCE_TOKEN.
   */
  token?: string;

  /**
   * The path at which the probe endpoint is mounted.
   * @default '/_hopfence/probe'
   */
  path?: string;

  /**
   * Optional list of allowed client IP addresses.
   * @default []
   */
  allowedIps?: string[];

  /**
   * Maximum requests per minute per IP.
   * @default 60
   */
  rateLimit?: number;
}

export interface HopFenceProbeResponse {
  schemaVersion: number;
  framework: 'express';
  request: {
    clientIp: string | null;
    socketPeerIp: string | null;
    scheme: string;
    host: string | null;
    pathBase: string;
    forwardedHeaders: {
      xForwardedForPresent: boolean;
      xForwardedProtoPresent: boolean;
      xForwardedHostPresent: boolean;
      rawXForwardedFor: string | null;
      rawXForwardedProto: string | null;
      rawXForwardedHost: string | null;
    };
  };
}

/**
 * Creates HopFence diagnostic probe middleware for Express.
 */
export function hopFence(options?: HopFenceOptions): RequestHandler;
export default hopFence;
