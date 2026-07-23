"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";

type TaskStats = {
    totalTasks: number;
    todoTasks: number;
    inProgressTasks: number;
    doneTasks: number;
    dueTodayTasks: number;
    overdueTasks: number;
};

type TaskItem = {
    id: string;
    title: string;
    description?: string | null;
    status: string;
    priority: string;
    dueDate: string;
    projectName?: string | null;
    tagNames?: string[];
    estimatedMinutes?: number | null;
    actualMinutes?: number;
    subtaskTotal?: number;
    subtaskDone?: number;
};

type TaskListResponse = {
    items: TaskItem[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
};

const emptyStats: TaskStats = {
    totalTasks: 0,
    todoTasks: 0,
    inProgressTasks: 0,
    doneTasks: 0,
    dueTodayTasks: 0,
    overdueTasks: 0,
};

const pageSize = 8;

const statusLabels: Record<string, string> = {
    Todo: "Chưa làm",
    InProgress: "Đang làm",
    Review: "Đang review",
    Done: "Hoàn thành",
};

const priorityLabels: Record<string, string> = {
    Low: "Thấp",
    Medium: "Trung bình",
    High: "Cao",
    Critical: "Khẩn cấp",
};

const statusClass = (status: string) => {
    switch (status) {
        case "Done":
            return "bg-emerald-100 text-emerald-700";
        case "Review":
            return "bg-violet-100 text-violet-700";
        case "InProgress":
            return "bg-sky-100 text-sky-700";
        default:
            return "bg-slate-100 text-slate-700";
    }
};

const priorityClass = (priority: string) => {
    switch (priority) {
        case "Critical":
            return "bg-rose-100 text-rose-700";
        case "High":
            return "bg-red-100 text-red-700";
        case "Medium":
            return "bg-amber-100 text-amber-700";
        default:
            return "bg-emerald-100 text-emerald-700";
    }
};

const getStartOfToday = () => {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
};

const getDateOnly = (value: string) => {
    const date = new Date(value);
    date.setHours(0, 0, 0, 0);
    return date;
};

const formatDate = (value: string) =>
    new Date(value).toLocaleDateString("vi-VN");

const getStatsFromTasks = (tasks: TaskItem[]) => {
    const today = getStartOfToday();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    return {
        totalTasks: tasks.length,
        todoTasks: tasks.filter(
            (task) => task.status === "Todo"
        ).length,
        inProgressTasks: tasks.filter(
            (task) =>
                task.status === "InProgress" ||
                task.status === "Review"
        ).length,
        doneTasks: tasks.filter(
            (task) => task.status === "Done"
        ).length,
        dueTodayTasks: tasks.filter((task) => {
            const dueDate = getDateOnly(task.dueDate);

            return (
                task.status !== "Done" &&
                dueDate >= today &&
                dueDate < tomorrow
            );
        }).length,
        overdueTasks: tasks.filter((task) => {
            const dueDate = getDateOnly(task.dueDate);

            return task.status !== "Done" && dueDate < today;
        }).length,
    };
};

export default function Dashboard() {
    const router = useRouter();

    const [tasks, setTasks] = useState<TaskItem[]>([]);
    const [stats, setStats] =
        useState<TaskStats>(emptyStats);
    const [totalCount, setTotalCount] = useState(0);
    const [errorMessage, setErrorMessage] = useState("");

    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("");
    const [priority, setPriority] = useState("");
    const [dueDate, setDueDate] = useState("");
    const [dueStatus, setDueStatus] = useState("");

    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);

    const fetchStats = useCallback(async () => {
        const res = await api.get<TaskStats>(
            "/api/tasks/stats"
        );

        return res.data;
    }, []);

    const fetchTasks = useCallback(async () => {
        const res = await api.get<
            TaskListResponse | TaskItem[]
        >(
            "/api/tasks",
            {
                params: {
                    search: search || undefined,
                    status: status || undefined,
                    priority: priority || undefined,
                    dueDate: dueDate || undefined,
                    dueStatus: dueStatus || undefined,
                    page,
                    pageSize,
                },
            }
        );

        if (Array.isArray(res.data)) {
            return {
                items: res.data,
                totalCount: res.data.length,
                page: 1,
                pageSize: res.data.length,
                totalPages: 1,
            };
        }

        return res.data;
    }, [
        search,
        status,
        priority,
        dueDate,
        dueStatus,
        page,
    ]);

    const loadDashboard = useCallback(async () => {
        const taskData = await fetchTasks();
        let nextStats = getStatsFromTasks(
            taskData.items ?? []
        );

        try {
            nextStats = await fetchStats();
        } catch (err) {
            console.log(err);
        }

        setTasks(taskData.items ?? []);
        setTotalCount(taskData.totalCount ?? 0);
        setTotalPages(
            Math.max(taskData.totalPages ?? 1, 1)
        );
        setStats(nextStats);
        setErrorMessage("");
    }, [fetchStats, fetchTasks]);

    useEffect(() => {
        let ignore = false;

        const load = async () => {
            try {
                const taskData = await fetchTasks();
                let nextStats = getStatsFromTasks(
                    taskData.items ?? []
                );

                try {
                    nextStats = await fetchStats();
                } catch (err) {
                    console.log(err);
                }

                if (!ignore) {
                    setTasks(taskData.items ?? []);
                    setTotalCount(
                        taskData.totalCount ?? 0
                    );
                    setTotalPages(
                        Math.max(
                            taskData.totalPages ?? 1,
                            1
                        )
                    );
                    setStats(nextStats);
                    setErrorMessage("");
                }
            } catch (err) {
                console.log(err);

                if (!ignore) {
                    setErrorMessage(
                        "Không tải được danh sách công việc. Vui lòng đăng nhập lại hoặc kiểm tra backend đang chạy."
                    );
                }
            }
        };

        void load();

        return () => {
            ignore = true;
        };
    }, [fetchStats, fetchTasks]);

    const deleteTask = async (id: string) => {
        try {
            await api.delete(`/api/tasks/${id}`);
            await loadDashboard();
        } catch (err) {
            console.log(err);
            setErrorMessage(
                "Không xóa được công việc. Vui lòng thử lại."
            );
        }
    };

    const exportCsv = async () => {
        try {
            const res = await api.get("/api/tasks/export.csv", {
                responseType: "blob",
            });
            const url = window.URL.createObjectURL(res.data);
            const link = document.createElement("a");

            link.href = url;
            link.download = "tasks.csv";
            link.click();
            window.URL.revokeObjectURL(url);
        } catch (err) {
            console.log(err);
            setErrorMessage(
                "Không xuất được CSV. Vui lòng thử lại."
            );
        }
    };

    const resetFilters = () => {
        setSearch("");
        setStatus("");
        setPriority("");
        setDueDate("");
        setDueStatus("");
        setPage(1);
    };

    const today = useMemo(() => getStartOfToday(), []);

    const getDueLabel = (task: TaskItem) => {
        if (task.status === "Done") {
            return {
                label: "Đã hoàn thành",
                className: "bg-emerald-50 text-emerald-700",
            };
        }

        const taskDate = getDateOnly(task.dueDate);

        if (taskDate < today) {
            return {
                label: "Quá hạn",
                className: "bg-red-100 text-red-700",
            };
        }

        if (taskDate.getTime() === today.getTime()) {
            return {
                label: "Hôm nay",
                className: "bg-amber-100 text-amber-700",
            };
        }

        return {
            label: "Còn hạn",
            className: "bg-slate-100 text-slate-600",
        };
    };

    const statCards = [
        {
            label: "Tổng công việc",
            value: stats.totalTasks,
            className: "text-slate-800",
        },
        {
            label: "Chưa làm",
            value: stats.todoTasks,
            className: "text-slate-800",
        },
        {
            label: "Đang làm",
            value: stats.inProgressTasks,
            className: "text-sky-700",
        },
        {
            label: "Hoàn thành",
            value: stats.doneTasks,
            className: "text-emerald-700",
        },
        {
            label: "Hôm nay",
            value: stats.dueTodayTasks,
            className: "text-amber-600",
        },
        {
            label: "Quá hạn",
            value: stats.overdueTasks,
            className: "text-red-600",
        },
    ];

    return (
        <div className="space-y-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold text-slate-800">
                        Bảng điều khiển
                    </h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Theo dõi và quản lý danh sách công việc của bạn
                    </p>
                </div>

                <div className="flex gap-2">
                    <button
                        onClick={exportCsv}
                        className="rounded-md border border-slate-200 bg-white px-4 py-2.5 text-sm font-bold text-slate-600 shadow-sm hover:bg-slate-50"
                    >
                        Xuất CSV
                    </button>
                    <button
                        onClick={() =>
                            router.push("/dashboard/create")
                        }
                        className="rounded-md bg-[#16b99f] px-4 py-2.5 text-sm font-bold text-white shadow-sm hover:bg-[#119f89]"
                    >
                        Tạo công việc
                    </button>
                </div>
            </div>

            <section className="grid gap-3 md:grid-cols-2 xl:grid-cols-6">
                {statCards.map((card) => (
                    <div
                        key={card.label}
                        className="rounded-md border border-slate-200 bg-white p-4 shadow-sm"
                    >
                        <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                            {card.label}
                        </p>
                        <p
                            className={`mt-2 text-3xl font-bold ${card.className}`}
                        >
                            {card.value}
                        </p>
                    </div>
                ))}
            </section>

            {errorMessage && (
                <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
                    {errorMessage}
                </div>
            )}

            <section className="rounded-md border border-slate-200 bg-white shadow-sm">
                <div className="border-b border-slate-200 p-4">
                    <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-[1.5fr_1fr_1fr_1fr_1fr_auto]">
                        <input
                            type="text"
                            placeholder="Tìm theo tiêu đề..."
                            value={search}
                            onChange={(e) => {
                                setSearch(e.target.value);
                                setPage(1);
                            }}
                            className="h-10 rounded-md border border-slate-200 px-3 text-sm outline-none focus:border-[#16b99f]"
                        />

                        <select
                            value={status}
                            onChange={(e) => {
                                setStatus(e.target.value);
                                setPage(1);
                            }}
                            className="h-10 rounded-md border border-slate-200 px-3 text-sm"
                        >
                            <option value="">
                                Tất cả trạng thái
                            </option>
                            <option value="Todo">Chưa làm</option>
                            <option value="InProgress">
                                Đang làm
                            </option>
                            <option value="Done">Hoàn thành</option>
                        </select>

                        <select
                            value={priority}
                            onChange={(e) => {
                                setPriority(e.target.value);
                                setPage(1);
                            }}
                            className="h-10 rounded-md border border-slate-200 px-3 text-sm"
                        >
                            <option value="">
                                Tất cả ưu tiên
                            </option>
                            <option value="Low">Thấp</option>
                            <option value="Medium">
                                Trung bình
                            </option>
                            <option value="High">Cao</option>
                        </select>

                        <input
                            type="date"
                            value={dueDate}
                            onChange={(e) => {
                                const value = e.target.value;

                                setDueDate(value);
                                if (value) {
                                    setDueStatus("");
                                }
                                setPage(1);
                            }}
                            className="h-10 rounded-md border border-slate-200 px-3 text-sm"
                        />

                        <select
                            value={dueStatus}
                            onChange={(e) => {
                                const value = e.target.value;

                                setDueStatus(value);
                                if (value) {
                                    setDueDate("");
                                }
                                setPage(1);
                            }}
                            className="h-10 rounded-md border border-slate-200 px-3 text-sm"
                        >
                            <option value="">Tất cả hạn</option>
                            <option value="today">Hôm nay</option>
                            <option value="overdue">Quá hạn</option>
                            <option value="thisWeek">
                                7 ngày tới
                            </option>
                        </select>

                        <button
                            onClick={resetFilters}
                            className="h-10 rounded-md border border-slate-200 px-4 text-sm font-semibold text-slate-600 hover:bg-slate-50"
                        >
                            Xóa lọc
                        </button>
                    </div>
                </div>

                <div className="overflow-x-auto">
                    <table className="w-full min-w-[900px] border-collapse text-sm">
                        <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
                            <tr>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    #
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Công việc
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Project
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Trạng thái
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Ưu tiên
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Hạn xử lý
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Tình trạng hạn
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Tiến độ
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3">
                                    Thời gian
                                </th>
                                <th className="border-b border-slate-200 px-4 py-3 text-right">
                                    Hành động
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            {tasks.length === 0 ? (
                                <tr>
                                    <td
                                        colSpan={10}
                                        className="px-4 py-12 text-center text-slate-400"
                                    >
                                        Không có công việc phù hợp
                                    </td>
                                </tr>
                            ) : (
                                tasks.map((task, index) => {
                                    const due = getDueLabel(task);

                                    return (
                                        <tr
                                            key={task.id}
                                            className="hover:bg-slate-50"
                                        >
                                            <td className="border-b border-slate-100 px-4 py-3 text-slate-400">
                                                {(page - 1) *
                                                    pageSize +
                                                    index +
                                                    1}
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3">
                                                <p className="font-semibold text-slate-800">
                                                    {task.title}
                                                </p>
                                                {task.description && (
                                                    <p className="mt-1 line-clamp-1 text-xs text-slate-500">
                                                        {
                                                            task.description
                                                        }
                                                    </p>
                                                )}
                                                {task.tagNames &&
                                                    task.tagNames.length >
                                                        0 && (
                                                        <div className="mt-2 flex flex-wrap gap-1">
                                                            {task.tagNames.map(
                                                                (tag) => (
                                                                    <span
                                                                        key={
                                                                            tag
                                                                        }
                                                                        className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-600"
                                                                    >
                                                                        {
                                                                            tag
                                                                        }
                                                                    </span>
                                                                )
                                                            )}
                                                        </div>
                                                    )}
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3 text-slate-600">
                                                {task.projectName ||
                                                    "Không có"}
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3">
                                                <span
                                                    className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${statusClass(
                                                        task.status
                                                    )}`}
                                                >
                                                    {statusLabels[
                                                        task.status
                                                    ] ??
                                                        task.status}
                                                </span>
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3">
                                                <span
                                                    className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${priorityClass(
                                                        task.priority
                                                    )}`}
                                                >
                                                    {priorityLabels[
                                                        task.priority
                                                    ] ??
                                                        task.priority}
                                                </span>
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3 text-slate-600">
                                                {formatDate(
                                                    task.dueDate
                                                )}
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3">
                                                <span
                                                    className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${due.className}`}
                                                >
                                                    {due.label}
                                                </span>
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3 text-slate-600">
                                                {(task.subtaskTotal ?? 0) >
                                                0
                                                    ? `${
                                                          task.subtaskDone ??
                                                          0
                                                      }/${
                                                          task.subtaskTotal
                                                      } checklist`
                                                    : "Chưa có checklist"}
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3 text-slate-600">
                                                {task.estimatedMinutes
                                                    ? `${
                                                          task.actualMinutes ??
                                                          0
                                                      }/${task.estimatedMinutes} phút`
                                                    : `${
                                                          task.actualMinutes ??
                                                          0
                                                      } phút`}
                                            </td>
                                            <td className="border-b border-slate-100 px-4 py-3">
                                                <div className="flex justify-end gap-2">
                                                    <button
                                                        onClick={() =>
                                                            router.push(
                                                                `/dashboard/edit/${task.id}`
                                                            )
                                                        }
                                                        className="rounded-md border border-sky-200 bg-sky-50 px-3 py-1.5 text-xs font-bold text-sky-700 hover:bg-sky-100"
                                                    >
                                                        Sửa
                                                    </button>
                                                    <button
                                                        onClick={() =>
                                                            deleteTask(
                                                                task.id
                                                            )
                                                        }
                                                        className="rounded-md border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-bold text-red-700 hover:bg-red-100"
                                                    >
                                                        Xóa
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })
                            )}
                        </tbody>
                    </table>
                </div>

                <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 px-4 py-3 text-sm">
                    <p className="text-slate-500">
                        Hiển thị {tasks.length} / {totalCount} công việc
                    </p>

                    <div className="flex items-center gap-2">
                        <button
                            disabled={page === 1}
                            onClick={() =>
                                setPage((current) =>
                                    Math.max(current - 1, 1)
                                )
                            }
                            className="rounded-md border border-slate-200 px-3 py-1.5 font-semibold text-slate-600 disabled:opacity-40"
                        >
                            Trước
                        </button>
                        <span className="rounded-md bg-[#16b99f] px-3 py-1.5 font-bold text-white">
                            {page} / {totalPages}
                        </span>
                        <button
                            disabled={page === totalPages}
                            onClick={() =>
                                setPage((current) =>
                                    Math.min(
                                        current + 1,
                                        totalPages
                                    )
                                )
                            }
                            className="rounded-md border border-slate-200 px-3 py-1.5 font-semibold text-slate-600 disabled:opacity-40"
                        >
                            Sau
                        </button>
                    </div>
                </div>
            </section>
        </div>
    );
}
