import { request } from "./client";

export interface GoalResponse {
  year: number;
  target: number | null;
  booksRead: number;
}

export function getGoals(): Promise<GoalResponse[]> {
  return request("/goals", "Failed to load your reading goals");
}

export function setGoal(year: number, target: number): Promise<GoalResponse> {
  return request(`/goals/${year}`, "Failed to save your goal", {
    method: "PUT",
    body: JSON.stringify({ target }),
  });
}

export function removeGoal(year: number): Promise<GoalResponse> {
  return request(`/goals/${year}`, "Failed to remove your goal", {
    method: "DELETE",
  });
}
