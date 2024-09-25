<Query Kind="Statements">
  <NuGetReference>Google.OrTools</NuGetReference>
  <Namespace>Google.OrTools.ConstraintSolver</Namespace>
  <Namespace>Google.Protobuf.WellKnownTypes</Namespace>
</Query>

//https://developers.google.cn/optimization/routing/pickup_delivery?authuser=0

//车辆路径规划 与任务分配
//任务分配和车辆是对应的，所以需要根据车辆的情况选择任务，进行分配。如果出现任务不在车辆内，就会异常。
//需要Distance 的capacity 值，不能太高也不能太低。

// 创建路由模型
// Instantiate the data problem.
DataModel data = new DataModel();

// Create Routing Index Manager
RoutingIndexManager manager =
	new RoutingIndexManager(data.DistanceMatrix.GetLength(0), data.VehicleNumber, data.Depot);

// Create Routing Model.
RoutingModel routing = new RoutingModel(manager);

//定义距离回调
//与 TSP 示例一样，以下函数会创建距离回调，该回调返回位置之间的距离并将其传递给求解器。它还将弧线成本（用于定义行程费用）设置为弧线的距离。

int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
														   {
															   // Convert from routing variable Index to
															   // distance matrix NodeIndex.
															   var fromNode = manager.IndexToNode(fromIndex);
															   var toNode = manager.IndexToNode(toIndex);
															   return data.DistanceMatrix[fromNode, toNode];
														   });
routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

//添加距离维度
//要解决此 VRP 问题，您需要创建一个距离维度，用于计算每辆车沿路线行驶的累计距离。
//然后，您可以设置与每条路线沿途总距离的最大值成比例的费用。路线规划程序使用维度来跟踪车辆行驶路线上累积的数量。
routing.AddDimension(
	transitCallbackIndex, // 过渡回调的索引，用于计算两个节点之间的距离  
	0,                    // slack_max，允许的最大松弛量，这里为0，表示不允许额外的松弛  
	int.MaxValue,         // capacity，维度的上限，这里设置每辆车的最大行驶距离为3000  
	true,                 // start_cumul_to_zero，是否将每辆车的起始累积值设为0  
	"Distance"            // 维度的名称  
);
RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
//方法为路线的全局跨度设置较大的系数 (100)，
//在车辆路径问题（VRP）中，RoutingDimension 通常用于跟踪某些累积量，例如距离、时间或载重。
//全局跨度（Global Span） 是指在所有车辆路径中，某个维度的最大值。这通常用来衡量最耗时或最耗费距离的车辆路线。
//通过将跨度（即所有车辆路径中该维度的最大值）乘以一个系数，并将其加入到优化目标函数中，从而鼓励求解器在优化时尽量减少这个跨度。
distanceDimension.SetGlobalSpanCostCoefficient(100);

//超过这个上限不会使解不可行，但会根据超过的数量施加惩罚成本。
//会按照设定的每单位惩罚成本（penalty cost per unit）累加到目标函数中。
for (int i = 0; i < data.VehicleNumber; i++)
{
	distanceDimension.SetSoftSpanUpperBoundForVehicle(new BoundCost(1000,10),i);
}


// Define Transportation Requests.
Solver solver = routing.solver();
for (int i = 0; i < data.PickupsDeliveries.GetLength(0); i++)
{
	long pickupIndex = manager.NodeToIndex(data.PickupsDeliveries[i][0]);
	long deliveryIndex = manager.NodeToIndex(data.PickupsDeliveries[i][1]);
	routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
	
	//以下一行添加了每个项目必须由同一辆车接送的要求。
	solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
	//每个物品在交付之前必须被取走。为此，我们要求车辆在物品取货地点的累积距离最多等于其在交付地点的累积距离。
	//先取后放
	solver.Add(solver.MakeLessOrEqual(distanceDimension.CumulVar(pickupIndex),
									  distanceDimension.CumulVar(deliveryIndex)));
									  
	routing.solver().Add(routing.NextVar(pickupIndex) == deliveryIndex);

	string taskType = data.TaskTypes[i];
	// 获取允许处理该任务类型的车辆类型列表  
	if (!data.TaskTypeToVehicleTypes.ContainsKey(taskType))
	{
		throw new Exception($"未定义的任务类型: {taskType}");
	}
	List<string> allowedVehicleTypes = data.TaskTypeToVehicleTypes[taskType];

	// 获取允许处理该任务类型的车辆索引列表  
	List<long> allowedVehicleIndices = new List<long>();
	for (int v = 0; v < data.VehicleNumber; v++)
	{
		if (allowedVehicleTypes.Contains(data.VehicleTypes[v]))
		{
			allowedVehicleIndices.Add(v);
		}
	}

	if (allowedVehicleIndices.Count() > 0) 
	{
		solver.Add(solver.MakeMemberCt(routing.VehicleVar(pickupIndex), allowedVehicleIndices.ToArray()));
	}

}

// Setting first solution heuristic.
RoutingSearchParameters searchParameters =
	operations_research_constraint_solver.DefaultRoutingSearchParameters();
searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;


// Solve the problem.
Assignment solution = routing.SolveWithParameters(searchParameters);

// Print solution on console.
PrintSolution(data, routing, manager, solution);

//添加解决方案打印机
/// <summary>
///   Print the solution.
/// </summary>
static void PrintSolution(in DataModel data, in RoutingModel routing, in RoutingIndexManager manager,
						  in Assignment solution)
{
	Console.WriteLine($"Objective {solution.ObjectiveValue()}:");

	// 构建节点与任务的映射  
	var nodeToTask = new Dictionary<int, (int taskIndex, bool isPickup)>();
	for (int t = 0; t < data.PickupsDeliveries.Length; t++)
	{
		nodeToTask[data.PickupsDeliveries[t][0]] = (t, true);  // 取货节点  
		nodeToTask[data.PickupsDeliveries[t][1]] = (t, false); // 送货节点  
	}

	// Inspect solution.
	long maxRouteDistance = 0;
	for (int i = 0; i < data.VehicleNumber; ++i)
	{
		var vehicleType = data.VehicleTypes[i];
		Console.WriteLine($"Route for Vehicle {i} (Type: {vehicleType}):");
		long routeDistance = 0;
		var index = routing.Start(i);
		while (routing.IsEnd(index) == false)
		{
			   int node = manager.IndexToNode((int)index);  

            // 检查当前节点是否对应某个任务  
            if (nodeToTask.ContainsKey(node))  
            {
				var (taskIndex, isPickup) = nodeToTask[node];
				string action = isPickup ? "Pickup" : "Delivery";
				string taskType = data.TaskTypes[taskIndex];
				Console.Write($"{node} ({action}, {taskType}) -> ");
			}
			else
			{
				// 如果不是任务节点，仅打印节点编号  
				Console.Write($"{node} -> ");
			}

			var previousIndex = index;
			index = solution.Value(routing.NextVar(index));

			// 计算路线距离，注意传递正确的车辆索引  
			routeDistance += routing.GetArcCostForVehicle(previousIndex, index, i);
		}
		Console.WriteLine("{0}", manager.IndexToNode((int)index));
		Console.WriteLine("Distance of the route: {0}m", routeDistance);
		maxRouteDistance = Math.Max(routeDistance, maxRouteDistance);
	}
	Console.WriteLine("Maximum distance of the routes: {0}m", maxRouteDistance);
}

//创建数据
//distance_matrix：一组位置之间的距离（以米为单位）。
//num_vehicles：车队中的车辆数量。
//depot：仓库的索引，这是所有车辆开始和结束路线的位置。
//PickupsDeliveries ：一组取货和送货地点对
class DataModel
{
	public long[,] DistanceMatrix = {
		{ 0, 548, 776, 696, 582, 274, 502, 194, 308, 194, 536, 502, 388, 354, 468, 776, 662 },
		{ 548, 0, 684, 308, 194, 502, 730, 354, 696, 742, 1084, 594, 480, 674, 1016, 868, 1210 },
		{ 776, 684, 0, 992, 878, 502, 274, 810, 468, 742, 400, 1278, 1164, 1130, 788, 1552, 754 },
		{ 696, 308, 992, 0, 114, 650, 878, 502, 844, 890, 1232, 514, 628, 822, 1164, 560, 1358 },
		{ 582, 194, 878, 114, 0, 536, 764, 388, 730, 776, 1118, 400, 514, 708, 1050, 674, 1244 },
		{ 274, 502, 502, 650, 536, 0, 228, 308, 194, 240, 582, 776, 662, 628, 514, 1050, 708 },
		{ 502, 730, 274, 878, 764, 228, 0, 536, 194, 468, 354, 1004, 890, 856, 514, 1278, 480 },
		{ 194, 354, 810, 502, 388, 308, 536, 0, 342, 388, 730, 468, 354, 320, 662, 742, 856 },
		{ 308, 696, 468, 844, 730, 194, 194, 342, 0, 274, 388, 810, 696, 662, 320, 1084, 514 },
		{ 194, 742, 742, 890, 776, 240, 468, 388, 274, 0, 342, 536, 422, 388, 274, 810, 468 },
		{ 536, 1084, 400, 1232, 1118, 582, 354, 730, 388, 342, 0, 878, 764, 730, 388, 1152, 354 },
		{ 502, 594, 1278, 514, 400, 776, 1004, 468, 810, 536, 878, 0, 114, 308, 650, 274, 844 },
		{ 388, 480, 1164, 628, 514, 662, 890, 354, 696, 422, 764, 114, 0, 194, 536, 388, 730 },
		{ 354, 674, 1130, 822, 708, 628, 856, 320, 662, 388, 730, 308, 194, 0, 342, 422, 536 },
		{ 468, 1016, 788, 1164, 1050, 514, 514, 662, 320, 274, 388, 650, 536, 342, 0, 764, 194 },
		{ 776, 868, 1552, 560, 674, 1050, 1278, 742, 1084, 810, 1152, 274, 388, 422, 764, 0, 798 },
		{ 662, 1210, 754, 1358, 1244, 708, 480, 856, 514, 468, 354, 844, 730, 536, 194, 798, 0 }
	};
	public int VehicleNumber = 4;
	public int Depot = 0;
	public int[][] PickupsDeliveries = {
			new int[] { 1, 6 }, 
			new int[] { 2, 10 },  new int[] { 4, 3 },   new int[] { 5, 9 },
			new int[] { 7, 8 }, new int[] { 15, 11 }, new int[] { 13, 12 }, new int[] { 16, 14 },
		};
	public string[] VehicleTypes = { "Small", "Large", "Large", "Large" };
	// 定义任务类型，每个任务对对应一个类型  
	public string[] TaskTypes = {
		"TypeA", // 对应 PickupsDeliveries[0]  
        "TypeB", // 对应 PickupsDeliveries[1]  
        "TypeA", // 对应 PickupsDeliveries[2]  
        "TypeC", // 对应 PickupsDeliveries[3]  
        "TypeB", // 对应 PickupsDeliveries[4]  
        "TypeA", // 对应 PickupsDeliveries[5]  
        "TypeC", // 对应 PickupsDeliveries[6]  
        "TypeB"  // 对应 PickupsDeliveries[7]
    };

	// 定义任务类型与车型的匹配关系  
	public Dictionary<string, List<string>> TaskTypeToVehicleTypes = new Dictionary<string, List<string>>()
	{
		{ "TypeA", new List<string> { "Small" } },
		{ "TypeB", new List<string> { "Large" } },
		{ "TypeC", new List<string> { "Large" } }
	};
};