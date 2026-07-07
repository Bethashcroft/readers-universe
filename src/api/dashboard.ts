import { request } from "./client";

export interface DashboardSummary {
  myBooks: number;
  nearby: number;
  pendingRequests: number;
}

export function getDashboardSummary(): Promise<DashboardSummary> {
  return request("/dashboard/summary", "Failed to load dashboard summary");
}
