"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";

type TaskItem = {
    id: string;
    title: string;
    description?: string | null;
    status: string;
    priority: string;
    dueDate: string;
    projectName?: string | null;
    tagNames?: string[];
};

type TaskListResponse = {
    items: TaskItem[];
};

const columns = [
    {
        status: "Todo",
        title: "Chưa làm",
    },
    {
        status: "InProgress",
        title: "Đang làm",
    },
    {
        status: "Review",
        title: "Review",
    },
    {
        status: "Done",
        title: "Hoàn thành",
    },
];

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

const priorityLabel = (priority: string) => {
    switch (priority) {
        case "Critical":
            return "Khẩn cấp";
        case "High":
            return "Cao";
        case "Medium":
            return "Trung bình";
        default:
            return "Thấp";
    }
};

export default function KanbanPage() {
    const router = useRouter();
    const [tasks, setTasks] = useState<TaskItem[]>([]);

    const fetchTasks = async () => {
        const res = await api.get<TaskListResponse>(
            "/api/tasks",
            {
                params: {
                    page: 1,
                    pageSize: 50,
                },
            }
        );

        return res.data.items ?? [];
    };

    const refreshTasks = async () => {
        const data = await fetchTasks();

        setTasks(data);
    };

    useEffect(() => {
        let ignore = false;

        const load = async () => {
            try {
                const data = await fetchTasks();

                if (!ignore) {
                    setTasks(data);
                }
            } catch (err) {
                console.log(err);
            }
        };

        void load();

        return () => {
            ignore = true;
        };
    }, []);

    const moveTask = async (task: TaskItem, status: string) => {
        try {
            await api.patch(`/api/tasks/${task.id}/status`, {
                status,
                sortOrder: 0,
            });

            await refreshTasks();
        } catch (err) {
            console.log(err);
            alert("Không chuyển được trạng thái");
        }
    };

    return (
        <div className="space-y-5">
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="text-2xl font-bold text-slate-800">
                        Kanban
                    </h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Theo dõi luồng công việc theo trạng thái.
                    </p>
                </div>
                <button
                    onClick={() =>
                        router.push("/dashboard/create")
                    }
                    className="rounded-md bg-[#16b99f] px-4 py-2.5 text-sm font-bold text-white hover:bg-[#119f89]"
                >
                    Tạo công việc
                </button>
            </div>

            <section className="grid gap-4 xl:grid-cols-4">
                {columns.map((column) => {
                    const columnTasks = tasks.filter(
                        (task) => task.status === column.status
                    );

                    return (
                        <div
                            key={column.status}
                            className="min-h-[520px] rounded-md border border-slate-200 bg-white shadow-sm"
                        >
                            <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
                                <h2 className="font-bold text-slate-800">
                                    {column.title}
                                </h2>
                                <span className="rounded-full bg-slate-100 px-2 py-1 text-xs font-bold text-slate-600">
                                    {columnTasks.length}
                                </span>
                            </div>

                            <div className="space-y-3 p-3">
                                {columnTasks.length === 0 ? (
                                    <div className="rounded-md border border-dashed border-slate-200 p-4 text-center text-sm text-slate-400">
                                        Không có công việc
                                    </div>
                                ) : (
                                    columnTasks.map((task) => (
                                        <article
                                            key={task.id}
                                            className="rounded-md border border-slate-200 bg-slate-50 p-3"
                                        >
                                            <div className="flex items-start justify-between gap-2">
                                                <h3 className="font-bold text-slate-800">
                                                    {task.title}
                                                </h3>
                                                <span
                                                    className={`shrink-0 rounded-full px-2 py-0.5 text-xs font-bold ${priorityClass(
                                                        task.priority
                                                    )}`}
                                                >
                                                    {priorityLabel(
                                                        task.priority
                                                    )}
                                                </span>
                                            </div>

                                            {task.description && (
                                                <p className="mt-2 line-clamp-2 text-sm text-slate-500">
                                                    {task.description}
                                                </p>
                                            )}

                                            <div className="mt-3 text-xs text-slate-500">
                                                Hạn:{" "}
                                                {new Date(
                                                    task.dueDate
                                                ).toLocaleDateString(
                                                    "vi-VN"
                                                )}
                                            </div>

                                            {task.projectName && (
                                                <div className="mt-1 text-xs font-semibold text-[#16b99f]">
                                                    {task.projectName}
                                                </div>
                                            )}

                                            {task.tagNames &&
                                                task.tagNames.length >
                                                    0 && (
                                                    <div className="mt-3 flex flex-wrap gap-1">
                                                        {task.tagNames.map(
                                                            (tag) => (
                                                                <span
                                                                    key={
                                                                        tag
                                                                    }
                                                                    className="rounded-full bg-white px-2 py-0.5 text-xs font-semibold text-slate-600"
                                                                >
                                                                    {
                                                                        tag
                                                                    }
                                                                </span>
                                                            )
                                                        )}
                                                    </div>
                                                )}

                                            <div className="mt-3 grid grid-cols-2 gap-2">
                                                {columns
                                                    .filter(
                                                        (target) =>
                                                            target.status !==
                                                            task.status
                                                    )
                                                    .map((target) => (
                                                        <button
                                                            key={
                                                                target.status
                                                            }
                                                            onClick={() =>
                                                                moveTask(
                                                                    task,
                                                                    target.status
                                                                )
                                                            }
                                                            className="rounded-md border border-slate-200 bg-white px-2 py-1.5 text-xs font-semibold text-slate-600 hover:border-[#16b99f] hover:text-[#16b99f]"
                                                        >
                                                            {target.title}
                                                        </button>
                                                    ))}
                                            </div>
                                        </article>
                                    ))
                                )}
                            </div>
                        </div>
                    );
                })}
            </section>
        </div>
    );
}
