"use client";

import { useCallback, useEffect, useState } from "react";
import { api } from "@/lib/api";

type Task = {
  id: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  dueDate: string;
  createdAt: string;
  userEmail: string;
  userName: string;
};

export default function AdminTasksPage() {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);

  const loadTasks = useCallback(async () => {
    try {
      const response = await api.get("/api/admin/tasks");

      setTasks(response.data);
    } catch (error) {
      console.error("Load tasks failed", error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadTasks();
  }, [loadTasks]);

  if (loading) {
    return (
      <div className="bg-white rounded-3xl shadow-md border border-slate-200 p-10 text-center">
        Loading tasks...
      </div>
    );
  }

  return (
    <div className="space-y-8">
      {/* Header */}

      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-4xl font-bold text-slate-800">All Tasks</h1>

          <p className="text-slate-500 mt-2">
            View and manage all tasks in the system
          </p>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl shadow-sm px-6 py-4">
          <p className="text-sm text-slate-500">Total Tasks</p>

          <h2 className="text-3xl font-bold text-blue-600">{tasks.length}</h2>
        </div>
      </div>

      {/* Table */}

      <div className="bg-white rounded-3xl border border-slate-200 shadow-md overflow-hidden">
        <div className="px-6 py-5 border-b border-slate-200">
          <h2 className="text-xl font-semibold">Task List</h2>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50">
              <tr>
                <th className="text-left px-6 py-4">Title</th>

                <th className="text-left px-6 py-4">User</th>

                <th className="text-left px-6 py-4">Status</th>

                <th className="text-left px-6 py-4">Priority</th>

                <th className="text-left px-6 py-4">Due Date</th>

                <th className="text-left px-6 py-4">Created At</th>
              </tr>
            </thead>

            <tbody>
              {tasks.map((task) => (
                <tr
                  key={task.id}
                  className="border-t border-slate-200 hover:bg-slate-50 transition"
                >
                  <td className="px-6 py-4 font-medium">{task.title}</td>

                  <td className="px-6 py-4">
                    <div>
                      <p className="font-medium">{task.userName}</p>

                      <p className="text-sm text-slate-500">{task.userEmail}</p>
                    </div>
                  </td>

                  <td className="px-6 py-4">
                    <span
                      className={
                        task.status === "Done"
                          ? "px-3 py-1 rounded-full bg-green-100 text-green-700 text-sm font-medium"
                          : task.status === "InProgress"
                            ? "px-3 py-1 rounded-full bg-blue-100 text-blue-700 text-sm font-medium"
                            : "px-3 py-1 rounded-full bg-slate-100 text-slate-700 text-sm font-medium"
                      }
                    >
                      {task.status}
                    </span>
                  </td>

                  <td className="px-6 py-4">
                    <span
                      className={
                        task.priority === "High"
                          ? "px-3 py-1 rounded-full bg-red-100 text-red-700 text-sm font-medium"
                          : task.priority === "Medium"
                            ? "px-3 py-1 rounded-full bg-yellow-100 text-yellow-700 text-sm font-medium"
                            : "px-3 py-1 rounded-full bg-green-100 text-green-700 text-sm font-medium"
                      }
                    >
                      {task.priority}
                    </span>
                  </td>

                  <td className="px-6 py-4 text-slate-600">
                    {new Date(task.dueDate).toLocaleDateString()}
                  </td>

                  <td className="px-6 py-4 text-slate-600">
                    {new Date(task.createdAt).toLocaleDateString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
