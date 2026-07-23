"use client";

import { useCallback, useEffect, useState } from "react";
import { api } from "@/lib/api";
import { useRouter } from "next/navigation";

type User = {
  id: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
};

export default function AdminPage() {
  const router = useRouter();

  const [users, setUsers] = useState<User[]>([]);

  const loadUsers = useCallback(async () => {
    try {
      const token = localStorage.getItem("token");

      const res = await api.get("/api/admin/users", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setUsers((res.data as User[]) ?? []);
    } catch (err) {
      console.log(err);
    }
  }, []);

  useEffect(() => {
    const role = localStorage.getItem("role");

    if (role !== "Admin") {
      alert("Access denied!");
      router.replace("/dashboard");
      return;
    }

    void loadUsers();
  }, [loadUsers, router]);

  const disableUser = async (id: string) => {
    const token = localStorage.getItem("token");

    await api.put(
      `/api/admin/users/${id}/disable`,
      {},
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    loadUsers();
  };

  const enableUser = async (id: string) => {
    const token = localStorage.getItem("token");

    await api.put(
      `/api/admin/users/${id}/enable`,
      {},
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    loadUsers();
  };
  const changeRole = async (id: string, role: string) => {
    const token = localStorage.getItem("token");

    await api.put(
      `/api/admin/users/${id}/role`,
      { role },
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    loadUsers();
  };

  useEffect(() => {
    loadUsers();
  }, []);

  return (
    <div className="space-y-8">
      {/* Header */}

      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-4xl font-bold text-slate-800">User Management</h1>

          <p className="text-slate-500 mt-2">
            Manage user accounts and permissions
          </p>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl shadow-sm px-6 py-4">
          <p className="text-sm text-slate-500">Total Users</p>

          <h2 className="text-3xl font-bold text-blue-600">{users.length}</h2>
        </div>
      </div>

      {/* Table */}

      <div className="bg-white rounded-3xl border border-slate-200 shadow-md overflow-hidden">
        <div className="px-6 py-5 border-b border-slate-200">
          <h2 className="text-xl font-semibold">Users List</h2>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50">
              <tr>
                <th className="text-left px-6 py-4">Full Name</th>

                <th className="text-left px-6 py-4">Email</th>

                <th className="text-left px-6 py-4">Role</th>

                <th className="text-left px-6 py-4">Status</th>

                <th className="text-center px-6 py-4">Actions</th>
              </tr>
            </thead>

            <tbody>
              {users.map((u) => (
                <tr
                  key={u.id}
                  className="border-t border-slate-200 hover:bg-slate-50"
                >
                  <td className="px-6 py-4 font-medium">{u.fullName}</td>

                  <td className="px-6 py-4 text-slate-600">{u.email}</td>

                  <td className="px-6 py-4">
                    <select
                      value={u.role}
                      onChange={(e) => changeRole(u.id, e.target.value)}
                      className="border border-slate-300 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="User">User</option>

                      <option value="Admin">Admin</option>
                    </select>
                  </td>

                  <td className="px-6 py-4">
                    <span
                      className={
                        u.isActive
                          ? "px-3 py-1 rounded-full bg-green-100 text-green-700 text-sm font-medium"
                          : "px-3 py-1 rounded-full bg-red-100 text-red-700 text-sm font-medium"
                      }
                    >
                      {u.isActive ? "Active" : "Disabled"}
                    </span>
                  </td>

                  <td className="px-6 py-4">
                    <div className="flex justify-center">
                      {u.isActive ? (
                        <button
                          onClick={() => disableUser(u.id)}
                          className="bg-red-500 hover:bg-red-600 text-white px-4 py-2 rounded-xl transition"
                        >
                          Disable
                        </button>
                      ) : (
                        <button
                          onClick={() => enableUser(u.id)}
                          className="bg-green-500 hover:bg-green-600 text-white px-4 py-2 rounded-xl transition"
                        >
                          Enable
                        </button>
                      )}
                    </div>
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
