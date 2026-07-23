"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";

type NavItem = {
    label: string;
    path: string;
    adminOnly?: boolean;
};

type UserInfo = {
    name: string;
    email: string;
    role: string;
};

const navItems: NavItem[] = [
    {
        label: "Bảng điều khiển",
        path: "/dashboard",
    },
    {
        label: "Tạo công việc",
        path: "/dashboard/create",
    },
    {
        label: "Project",
        path: "/dashboard/projects",
    },
    {
        label: "Kanban",
        path: "/dashboard/kanban",
    },
    {
        label: "Chat",
        path: "/dashboard/chat",
    },
    {
        label: "Người dùng",
        path: "/dashboard/admin",
        adminOnly: true,
    },
    {
        label: "Tất cả công việc",
        path: "/dashboard/admin/tasks",
        adminOnly: true,
    },
];

const readJwtPayload = (token: string | null) => {
    if (!token) {
        return {};
    }

    try {
        const payload = token.split(".")[1];
        const normalized = payload
            .replace(/-/g, "+")
            .replace(/_/g, "/");
        const json = window.atob(normalized);

        return JSON.parse(json) as Record<string, unknown>;
    } catch {
        return {};
    }
};

const readTextClaim = (
    payload: Record<string, unknown>,
    keys: string[]
) => {
    for (const key of keys) {
        const value = payload[key];

        if (typeof value === "string" && value.trim()) {
            return value;
        }
    }

    return "";
};

const getUserInfo = (): UserInfo => {
    const token = localStorage.getItem("token");
    const payload = readJwtPayload(token);

    const email =
        localStorage.getItem("userEmail") ||
        readTextClaim(payload, [
            "email",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress",
        ]);

    const name =
        localStorage.getItem("userName") ||
        readTextClaim(payload, [
            "name",
            "unique_name",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
        ]) ||
        email ||
        "Người dùng";

    const role =
        localStorage.getItem("role") ||
        readTextClaim(payload, [
            "role",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
        ]) ||
        "User";

    return {
        name,
        email,
        role,
    };
};

export default function DashboardLayout({
    children,
}: {
    children: React.ReactNode;
}) {
    const router = useRouter();
    const pathname = usePathname();

    const [user, setUser] = useState<UserInfo>({
        name: "Người dùng",
        email: "",
        role: "User",
    });

    useEffect(() => {
        const timeoutId = window.setTimeout(() => {
            setUser(getUserInfo());
        }, 0);

        return () => window.clearTimeout(timeoutId);
    }, []);

    const visibleItems = navItems.filter(
        (item) => !item.adminOnly || user.role === "Admin"
    );

    const isActive = (path: string) => {
        if (path === "/dashboard") {
            return (
                pathname === "/dashboard" ||
                pathname.startsWith("/dashboard/edit")
            );
        }

        if (path === "/dashboard/create") {
            return pathname.startsWith("/dashboard/create");
        }

        if (path === "/dashboard/projects") {
            return pathname.startsWith("/dashboard/projects");
        }

        if (path === "/dashboard/kanban") {
            return pathname.startsWith("/dashboard/kanban");
        }

        if (path === "/dashboard/chat") {
            return pathname.startsWith("/dashboard/chat");
        }

        return pathname === path;
    };

    return (
        <div className="min-h-screen bg-[#eef3f5] text-slate-700">
            <div className="flex min-h-screen">
                <aside className="fixed left-0 top-0 z-30 flex h-screen w-72 flex-col border-r border-slate-200 bg-white shadow-sm">
                    <div className="border-b border-slate-200 px-6 py-5">
                        <p className="text-xs font-bold uppercase tracking-wide text-[#16b99f]">
                            TaskFlow
                        </p>
                        <h1 className="mt-1 text-2xl font-bold text-slate-800">
                            Bảng điều khiển
                        </h1>
                    </div>

                    <nav className="flex-1 space-y-2 p-4">
                        {visibleItems.map((item) => (
                            <button
                                key={`${item.label}-${item.path}`}
                                onClick={() =>
                                    router.push(item.path)
                                }
                                className={`w-full rounded-md px-4 py-3 text-left text-sm font-semibold transition ${
                                    isActive(item.path)
                                        ? "bg-[#16b99f] text-white shadow-sm"
                                        : "text-slate-600 hover:bg-slate-100"
                                }`}
                            >
                                {item.label}
                            </button>
                        ))}
                    </nav>

                    <div className="border-t border-slate-200 p-4">
                        <div className="mb-3 rounded-md bg-slate-50 p-3">
                            <p className="truncate text-sm font-bold text-slate-800">
                                {user.name}
                            </p>
                            {user.email && user.email !== user.name && (
                                <p className="mt-0.5 truncate text-xs text-slate-500">
                                    {user.email}
                                </p>
                            )}
                            <p className="mt-2 text-xs font-semibold text-[#16b99f]">
                                {user.role}
                            </p>
                        </div>

                        <button
                            onClick={() => {
                                localStorage.clear();
                                router.push("/login");
                            }}
                            className="w-full rounded-md bg-red-500 px-4 py-3 text-sm font-bold text-white transition hover:bg-red-600"
                        >
                            Đăng xuất
                        </button>
                    </div>
                </aside>

                <main className="ml-72 min-w-0 flex-1 p-5">
                    {children}
                </main>
            </div>
        </div>
    );
}
